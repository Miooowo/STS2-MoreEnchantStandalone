using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;

namespace MoreEnchant.Standalone.Compat;

/// <summary>检测那维莱特模组（CharMod）是否已加载；潮涌语义与模组内 <c>SurgeCard.ApplySurgeLogic</c> 一致：先回复生命再施加潮落（<c>SurgePower</c>）。</summary>
internal static class NeuvilletteCompat
{
	private const string NeuvilletteAssemblyName = "CharMod";
	private const string SurgePowerTypeName = "NeuvilletteMod.NeuvilletteModCode.Powers.SurgePower";
	private const string LivingWaterPowerTypeName = "NeuvilletteMod.NeuvilletteModCode.Powers.LivingWaterPower";
	/// <summary>模组根命名空间下的 <c>NeuvilletteKeywords</c>（与 <c>NeuvilletteKeywords.Surge</c> 一致）。</summary>
	private const string NeuvilletteKeywordsTypeName = "NeuvilletteKeywords";
	private const string SurgeIconResource = "res://NeuvilletteMod/images/powers/SurgePower.png";

	private static bool IsNeuvilletteAssemblyName(string? name) =>
		string.Equals(name, NeuvilletteAssemblyName, StringComparison.OrdinalIgnoreCase)
		|| string.Equals(name, "NeuvilletteMod", StringComparison.OrdinalIgnoreCase);

	internal static bool IsNeuvilletteModAvailable()
	{
		if (ResourceLoader.Exists(SurgeIconResource))
			return true;

		return AppDomain.CurrentDomain.GetAssemblies().Any(IsNeuvilletteAssemblyLoaded);
	}

	private static bool IsNeuvilletteAssemblyLoaded(Assembly a) =>
		IsNeuvilletteAssemblyName(a.GetName().Name);

	private static Assembly? TryGetNeuvilletteAssembly()
	{
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (IsNeuvilletteAssemblyName(asm.GetName().Name))
				return asm;
		}

		return null;
	}

	internal static Type? ResolveSurgePowerType()
	{
		var asm = TryGetNeuvilletteAssembly();
		var t = asm?.GetType(SurgePowerTypeName);
		if (t != null)
			return t;
		return Type.GetType($"{SurgePowerTypeName}, {NeuvilletteAssemblyName}")
			?? Type.GetType($"{SurgePowerTypeName}, NeuvilletteMod");
	}

	private static Type? ResolveLivingWaterPowerType() =>
		TryGetNeuvilletteAssembly()?.GetType(LivingWaterPowerTypeName);

	private static Type? ResolveNeuvilletteKeywordsType()
	{
		var asm = TryGetNeuvilletteAssembly();
		if (asm != null)
		{
			foreach (var name in new[] { NeuvilletteKeywordsTypeName, "NeuvilletteMod.NeuvilletteKeywords" })
			{
				var t = asm.GetType(name);
				if (t != null)
					return t;
			}
		}

		return Type.GetType($"{NeuvilletteKeywordsTypeName}, {NeuvilletteAssemblyName}")
			?? Type.GetType($"{NeuvilletteKeywordsTypeName}, NeuvilletteMod");
	}

	/// <summary>模组自定义关键词「潮涌」（<c>NeuvilletteKeywords.Surge</c>）。</summary>
	internal static bool TryGetNeuvilletteSurgeKeyword(out CardKeyword surgeKeyword)
	{
		surgeKeyword = default;
		var t = ResolveNeuvilletteKeywordsType();
		if (t == null)
			return false;
		var f = t.GetField("Surge", BindingFlags.Public | BindingFlags.Static);
		if (f?.GetValue(null) is not CardKeyword kw)
			return false;
		surgeKeyword = kw;
		return true;
	}

	/// <summary>与模组 <c>SurgeCard.ApplySurgeLogic</c> 相同：基础潮涌值 + 每层 <c>LivingWaterPower</c> 的 <c>Amount</c>。</summary>
	internal static decimal ComputeSurgeHealAndTideAmount(Creature creature, decimal baseSurge)
	{
		var total = baseSurge;
		var livingType = ResolveLivingWaterPowerType();
		if (livingType == null)
			return total;

		foreach (var power in creature.Powers)
		{
			if (!livingType.IsInstanceOfType(power))
				continue;
			total += power.Amount;
		}

		return total;
	}

	/// <summary>先 <see cref="CreatureCmd.Heal"/> 再施加潮落（<c>SurgePower</c>），层数与回复量一致。</summary>
	internal static async Task ApplySurgeHealThenTide(Creature creature, decimal baseSurge, CardModel? source)
	{
		if (!IsNeuvilletteModAvailable() || creature.CombatState == null || baseSurge == 0m)
			return;

		var amount = ComputeSurgeHealAndTideAmount(creature, baseSurge);
		await CreatureCmd.Heal(creature, amount, playAnim: true);
		await TryApplySurgePower(creature, amount, source);
	}

	/// <summary>
	/// 施加潮落（<c>SurgePower</c>）。不用泛型 <c>PowerCmd.Apply&lt;T&gt;</c>，否则 <c>ModelDb.Power&lt;T&gt;</c> 在跨程序集 <c>T</c> 上可能无法解析；改为 <see cref="ModelDb.DebugPower"/> + <c>PowerCmd.Apply(PowerModel, …)</c>。
	/// </summary>
	internal static async Task TryApplySurgePower(Creature creature, decimal amount, CardModel? source)
	{
		if (!IsNeuvilletteModAvailable() || creature.CombatState == null || amount == 0m)
			return;

		var surgeType = ResolveSurgePowerType();
		if (surgeType == null || !typeof(PowerModel).IsAssignableFrom(surgeType))
			return;
		if (!ModelDb.Contains(surgeType))
			return;

		var template = ModelDb.DebugPower(surgeType);
		var power = template.ToMutable();
		await PowerCmd.Apply(power, creature, amount, creature, source, false);
	}
}
