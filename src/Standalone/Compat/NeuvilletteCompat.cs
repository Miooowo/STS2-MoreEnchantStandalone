using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MoreEnchant.Compat;

namespace MoreEnchant.Standalone.Compat;

/// <summary>检测那维莱特模组（CharMod）是否已加载；潮涌语义与模组内 <c>SurgeCard.ApplySurgeLogic</c> 一致：先回复生命再施加潮落（<c>SurgePower</c>）。</summary>
internal static class NeuvilletteCompat
{
	private const string NeuvilletteAssemblyName = "Neuvillette";
	private const string SurgePowerTypeName = "Neuvillette.Characters.Neuvillette.Powers.SurgePower";
	private const string LivingWaterPowerTypeName = "Neuvillette.Characters.Neuvillette.Powers.LivingWaterPower";
	private const string OratricePowerTypeName = "Neuvillette.Characters.Neuvillette.Powers.OratricePower";
	private const string ProceduralJusticePowerTypeName = "Neuvillette.Characters.Neuvillette.Powers.ProceduralJusticePower";
	private const string MelusineCardPoolTypeName = "Neuvillette.Characters.Neuvillette.MelusineCardPool";
	private const string NeuvilletteKeywordsTypeName = "Neuvillette.Characters.Neuvillette.Cards.NeuvilletteKeywords";
	private const string NeuvilletteRootResource = "res://Neuvillette/";

	private static bool IsNeuvilletteAssemblyName(string? name) =>
		string.Equals(name, NeuvilletteAssemblyName, StringComparison.OrdinalIgnoreCase)
		|| string.Equals(name, "NeuvilletteMod", StringComparison.OrdinalIgnoreCase);

	internal static bool IsNeuvilletteModAvailable()
	{
		if (ResourceLoader.Exists(NeuvilletteRootResource))
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
		return TryGetNeuvilletteKeyword("SurgeKeyword", "Surge", out surgeKeyword);
	}

	/// <summary>模组自定义关键词「呈堂」（<c>NeuvilletteKeywords.Submit</c>）。</summary>
	internal static bool TryGetSubmitKeyword(out CardKeyword submitKeyword)
	{
		return TryGetNeuvilletteKeyword("SubmitKeyword", "Submit", out submitKeyword);
	}

	/// <summary>模组自定义关键词「美露莘贴纸」（<c>NeuvilletteKeywords.MelusineSticker</c>）。</summary>
	internal static bool TryGetMelusineStickerKeyword(out CardKeyword stickerKeyword)
	{
		return TryGetNeuvilletteKeyword("MelusineStickerKeyword", "MelusineSticker", out stickerKeyword);
	}

	private static bool TryGetNeuvilletteKeyword(string cardKeywordFieldName, string keywordIdFieldName, out CardKeyword keyword)
	{
		keyword = default;
		var t = ResolveNeuvilletteKeywordsType();
		if (t == null)
			return false;

		var cardKeywordField = t.GetField(cardKeywordFieldName, BindingFlags.Public | BindingFlags.Static);
		if (cardKeywordField?.GetValue(null) is CardKeyword directKeyword)
		{
			keyword = directKeyword;
			return true;
		}

		var keywordIdField = t.GetField(keywordIdFieldName, BindingFlags.Public | BindingFlags.Static);
		if (keywordIdField?.GetValue(null) is string keywordId
		    && TryResolveModKeyword(keywordId, out var resolvedKeyword))
		{
			keyword = resolvedKeyword;
			return true;
		}

		return false;
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
		await PowerCmdCompat.Apply(power, creature, amount, creature, source, null, false);
	}

	internal static async Task<bool> TryGrantRandomMelusineSticker(Player owner, CardModel? source)
	{
		var combatState = owner.Creature.CombatState;
		if (!IsNeuvilletteModAvailable() || combatState == null)
			return false;

		var melusinePoolType = ResolveType(MelusineCardPoolTypeName);
		if (melusinePoolType == null)
			return false;

		var candidates = ModelDb.AllCards
			.Where(card => card?.Pool != null && melusinePoolType.IsInstanceOfType(card.Pool))
			.ToList();
		if (candidates.Count == 0)
			return false;

		var generated = CardFactory.GetForCombat(owner, candidates, 1, owner.RunState.Rng.CombatCardGeneration)
			.FirstOrDefault();
		if (generated == null)
			return false;

		await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, owner);
		return true;
	}

	internal static async Task<bool> TrySubmitCardFromDiscard(
		PlayerChoiceContext choiceContext,
		Player owner,
		CardModel? source)
	{
		if (!IsNeuvilletteModAvailable() || owner.Creature.CombatState == null)
			return false;

		var discardCards = PileType.Discard.GetPile(owner).Cards.ToList();
		if (discardCards.Count == 0)
			return false;

		var selected = (await CardSelectCmd.FromSimpleGrid(
			choiceContext,
			discardCards,
			owner,
			new CardSelectorPrefs(new LocString("card_selection", "TO_SUBMIT"), 1)))
			.FirstOrDefault();
		if (selected == null)
			return false;

		var resolvedCost = selected.EnergyCost == null ? 0 : Math.Max(0, (int)selected.EnergyCost.GetResolved());
		var points = 10 + resolvedCost * 10;

		await CardCmd.Exhaust(choiceContext, selected);
		await TryApplyNeuvilletteCounterPower(owner.Creature, OratricePowerTypeName, points, source);

		var proceduralType = ResolveType(ProceduralJusticePowerTypeName);
		if (proceduralType != null)
		{
			var bonus = owner.Creature.Powers.FirstOrDefault(proceduralType.IsInstanceOfType)?.Amount ?? 0m;
			if (bonus > 0m)
				await TryApplyNeuvilletteCounterPower(owner.Creature, OratricePowerTypeName, bonus, source);
		}

		return true;
	}

	private static async Task TryApplyNeuvilletteCounterPower(
		Creature creature,
		string powerTypeName,
		decimal amount,
		CardModel? source)
	{
		if (amount <= 0m)
			return;

		var powerType = ResolveType(powerTypeName);
		if (powerType == null || !typeof(PowerModel).IsAssignableFrom(powerType))
			return;
		if (!ModelDb.Contains(powerType))
			return;

		var template = ModelDb.DebugPower(powerType);
		var power = template.ToMutable();
		await PowerCmdCompat.Apply(power, creature, amount, creature, source, null, false);
	}

	private static Type? ResolveType(string typeName)
	{
		var asm = TryGetNeuvilletteAssembly();
		if (asm != null)
		{
			var hit = asm.GetType(typeName);
			if (hit != null)
				return hit;
		}

		return Type.GetType($"{typeName}, {NeuvilletteAssemblyName}")
			?? Type.GetType($"{typeName}, NeuvilletteMod")
			?? Type.GetType($"{typeName}, CharMod");
	}

	private static bool TryResolveModKeyword(string keywordId, out CardKeyword keyword)
	{
		keyword = default;
		if (string.IsNullOrWhiteSpace(keywordId))
			return false;

		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			var asmName = asm.GetName().Name;
			if (string.IsNullOrWhiteSpace(asmName) ||
			    !asmName.Contains("RitsuLib", StringComparison.OrdinalIgnoreCase))
				continue;

			foreach (var type in asm.GetTypes())
			{
				var method = type.GetMethod(
					"GetModCardKeyword",
					BindingFlags.Public | BindingFlags.Static,
					null,
					[typeof(string)],
					null);
				if (method == null || method.ReturnType != typeof(CardKeyword))
					continue;

				if (method.Invoke(null, [keywordId]) is CardKeyword resolved)
				{
					keyword = resolved;
					return true;
				}
			}
		}

		return false;
	}
}
