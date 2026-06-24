using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone.Compat;

public enum HextechRuneTier
{
	Silver,
	Gold,
	Prismatic,
}

/// <summary>海克斯符文模组联动：检测可用性、发放锻造器/海克斯、战后移除临时海克斯（反射调用，避免硬依赖）。</summary>
internal static class HextechRunesCompat
{
	private const string HextechAssemblyName = "HextechRunes";
	private const string RuneGrantHelperTypeName = "HextechRunes.HextechRuneGrantHelper";
	private const string ForgeGrantHelperTypeName = "HextechRunes.HextechForgeGrantHelper";
	private const string CatalogTypeName = "HextechRunes.HextechCatalog";
	private const string RarityTierTypeName = "HextechRunes.HextechRarityTier";

	private static Assembly? TryGetHextechAssembly()
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(a => string.Equals(a.GetName().Name, HextechAssemblyName, StringComparison.OrdinalIgnoreCase));
	}

	private static Type? ResolveType(string fullTypeName)
	{
		var asm = TryGetHextechAssembly();
		return asm?.GetType(fullTypeName) ?? Type.GetType($"{fullTypeName}, {HextechAssemblyName}");
	}

	internal static bool IsHextechRunesModAvailable() =>
		ResolveType(RuneGrantHelperTypeName) != null
		&& ResolveType(ForgeGrantHelperTypeName) != null
		&& ResolveType(CatalogTypeName) != null
		&& ResolveType(RarityTierTypeName) != null;

	internal static async Task<bool> TryGrantRandomForge(Player player)
	{
		if (!IsHextechRunesModAvailable())
			return false;

		var helperType = ResolveType(ForgeGrantHelperTypeName);
		var method = helperType?.GetMethod(
			"ObtainRandomForges",
			BindingFlags.Public | BindingFlags.Static,
			null,
			[typeof(Player), typeof(int)],
			null);
		if (method == null)
			return false;

		try
		{
			await (Task)method.Invoke(null, [player, 1])!;
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static async Task<bool> TryGrantRandomForgeAfterUiFrame(Player player)
	{
		if (Engine.GetMainLoop() is SceneTree tree)
			await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		return await TryGrantRandomForge(player);
	}

	internal static async Task<ModelId?> TryGrantTemporaryRune(Player player, HextechRuneTier tier)
	{
		if (!IsHextechRunesModAvailable())
			return null;

		var catalogType = ResolveType(CatalogTypeName);
		var tierType = ResolveType(RarityTierTypeName);
		var helperType = ResolveType(RuneGrantHelperTypeName);
		if (catalogType == null || tierType == null || helperType == null)
			return null;

		var getPool = catalogType.GetMethod(
			"GetPlayerRuneTypesForRarity",
			BindingFlags.Public | BindingFlags.Static,
			null,
			[tierType],
			null);
		var grant = helperType.GetMethod(
			"ObtainRandomRunes",
			BindingFlags.Public | BindingFlags.Static,
			null,
			[typeof(Player), typeof(IEnumerable<Type>), typeof(int)],
			null);
		var isHextechRelic = catalogType.GetMethod(
			"IsHextechRelic",
			BindingFlags.Public | BindingFlags.Static,
			null,
			[typeof(RelicModel)],
			null);
		if (getPool == null || grant == null || isHextechRelic == null)
			return null;

		var tierName = tier switch
		{
			HextechRuneTier.Silver => "Silver",
			HextechRuneTier.Gold => "Gold",
			HextechRuneTier.Prismatic => "Prismatic",
			_ => "Silver",
		};
		var tierValue = Enum.Parse(tierType, tierName);
		if (tierValue == null)
			return null;

		var before = player.Relics.ToHashSet();
		try
		{
			var pool = getPool.Invoke(null, [tierValue]);
			if (pool is not IEnumerable<Type> candidateTypes)
				return null;

			await (Task)grant.Invoke(null, [player, candidateTypes, 1])!;
		}
		catch
		{
			return null;
		}

		foreach (var relic in player.Relics)
		{
			if (before.Contains(relic))
				continue;
			if (isHextechRelic.Invoke(null, [relic]) is true)
				return relic.Id;
		}

		return null;
	}

	internal static async Task RemoveTemporaryRunes(Player player, List<ModelId> temporaryRunes)
	{
		if (temporaryRunes.Count == 0)
			return;

		// 不能按对象引用匹配：跨房间后 relic 实例可能重建，改为按 ModelId 且按数量移除。
		var available = player.Relics.ToList();
		var toRemove = new List<RelicModel>();
		foreach (var pendingId in temporaryRunes)
		{
			var matchIndex = available.FindIndex(r => r.Id == pendingId);
			if (matchIndex < 0)
				continue;
			toRemove.Add(available[matchIndex]);
			available.RemoveAt(matchIndex);
		}

		temporaryRunes.Clear();
		foreach (var relic in toRemove)
			await RelicCmd.Remove(relic);
	}
}
