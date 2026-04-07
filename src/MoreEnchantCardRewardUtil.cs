using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Enchantments.Mocks;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.Enchantments;

namespace MoreEnchant;

internal static class MoreEnchantCardRewardUtil
{

	public static void ApplyRandomEnchantments(Player player, List<CardCreationResult> results,
		CardCreationOptions options)
	{
		if (options.Flags.HasFlag(CardCreationFlags.NoModifyHooks))
			return;
		if (options.Source == CardCreationSource.Shop)
			return;

		var rng = player.PlayerRng.Rewards;
		var templates = ModelDb.DebugEnchantments.Where(IsEligibleRewardTemplate).ToArray();
		var settings = MoreEnchantSettingsStore.Get();

		if (MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardBellCurse &&
		    options.Source == CardCreationSource.Encounter)
		{
			MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardBellCurse = false;
			TryApplyForcedBellCurseCardReward(player, results, rng);
		}

		foreach (var result in results)
		{
			var card = result.Card;
			if (card.Enchantment != null)
				continue;
			if (ShouldSkipEnchantingRewardCard(card))
				continue;

			var chancePercent = Math.Clamp(settings.RewardEnchantChancePercent, 0, 100);
			if (chancePercent <= 0 || rng.NextInt(0, 100) >= chancePercent)
				continue;

			var pick = RollEnchantmentTemplate(card, templates, rng, settings);
			if (pick == null)
				continue;

			var enchant = (EnchantmentModel)pick.MutableClone();
			var amount = RollEnchantAmount(rng, pick);

			CardCmd.Enchant(enchant, card, amount);
			if (enchant is BellCurseEnchantment)
				BellCurseReward.MarkPendingRelicGrant(card);
		}
	}

	private static (float Common, float Uncommon, float Curse, float Rare, float Special) GetEffectiveBucketWeights(
		CardModel card,
		MoreEnchantSettings settings)
	{
		if (settings.UseChimeraRarityByCardRarity)
			return GetBucketWeightsForCardRarity(card.Rarity);
		var wc = settings.WeightCurse > 0 ? settings.WeightCurse : 250;
		return (
			Math.Max(0f, settings.WeightCommon),
			Math.Max(0f, settings.WeightUncommon),
			Math.Max(0f, wc),
			Math.Max(0f, settings.WeightRare),
			Math.Max(0f, settings.WeightSpecial));
	}

	/// <summary>与 ChimeraTheSpire 奇美拉卡牌修饰的按卡牌稀有度分桶权重一致（插入诅咒档于罕见与稀有之间）。</summary>
	private static (float Common, float Uncommon, float Curse, float Rare, float Special) GetBucketWeightsForCardRarity(
		CardRarity rarity)
	{
		switch (rarity)
		{
			case CardRarity.Basic:
			case CardRarity.Common:
				return (0.50f, 0.30f, 0.12f, 0.079f, 0.001f);
			case CardRarity.Uncommon:
				return (0.30f, 0.40f, 0.15f, 0.14f, 0.01f);
			case CardRarity.Rare:
				return (0.25f, 0.30f, 0.35f, 0.05f, 0.05f);
			case CardRarity.Event:
			case CardRarity.Curse:
				return (0.3244f, 0.3333f, 0.24f, 0.10f, 0.0023f);
			case CardRarity.Ancient:
			case CardRarity.Quest:
				return (0.20f, 0.30f, 0.35f, 0.05f, 0.10f);
			default:
				return (0.50f, 0.30f, 0.12f, 0.079f, 0.001f);
		}
	}

	private static EnchantmentRewardRarity GetRewardRarity(EnchantmentModel template)
	{
		if (template is Clone or TezcatarasEmber or Goopy or Glam)
			return EnchantmentRewardRarity.Special;
		if (template is IRewardEnchantRarity withRarity)
			return withRarity.RewardRarity;
		return EnchantmentRewardRarity.Common;
	}

	private static bool IsEligibleRewardTemplate(EnchantmentModel t) =>
		t is not DeprecatedEnchantment and not MockFreeEnchantment;

	private static bool ShouldSkipEnchantingRewardCard(CardModel card) =>
		card.Rarity is CardRarity.Token or CardRarity.Status;

	private static EnchantmentModel? RollEnchantmentTemplate(CardModel card, EnchantmentModel[] templates,
		Rng rng,
		MoreEnchantSettings settings)
	{
		var byRarity = new Dictionary<EnchantmentRewardRarity, List<EnchantmentModel>>();
		foreach (var t in templates)
		{
			if (!t.CanEnchant(card))
				continue;

			var r = GetRewardRarity(t);
			if (!byRarity.TryGetValue(r, out var list))
			{
				list = [];
				byRarity[r] = list;
			}

			list.Add(t);
		}

		if (byRarity.Count == 0)
			return null;

		var (wCommon, wUncommon, wCurse, wRare, wSpecial) = GetEffectiveBucketWeights(card, settings);

		bool hasCommon = byRarity.TryGetValue(EnchantmentRewardRarity.Common, out var listCommon) &&
		                 listCommon.Count > 0;
		bool hasUncommon = byRarity.TryGetValue(EnchantmentRewardRarity.Uncommon, out var listUncommon) &&
		                   listUncommon.Count > 0;
		bool hasCurse = byRarity.TryGetValue(EnchantmentRewardRarity.Curse, out var listCurse) && listCurse.Count > 0;
		bool hasRare = byRarity.TryGetValue(EnchantmentRewardRarity.Rare, out var listRare) && listRare.Count > 0;
		bool hasSpecial = byRarity.TryGetValue(EnchantmentRewardRarity.Special, out var listSpecial) &&
		                  listSpecial.Count > 0;

		if (!hasCommon) wCommon = 0f;
		if (!hasUncommon) wUncommon = 0f;
		if (!hasCurse) wCurse = 0f;
		if (!hasRare) wRare = 0f;
		if (!hasSpecial) wSpecial = 0f;

		var sum = wCommon + wUncommon + wCurse + wRare + wSpecial;
		if (sum <= 0f)
		{
			var all = byRarity.Values.SelectMany(x => x).ToList();
			return rng.NextItem(all);
		}

		var roll = rng.NextFloat() * sum;
		EnchantmentRewardRarity bucket;
		if ((roll -= wCommon) < 0f) bucket = EnchantmentRewardRarity.Common;
		else if ((roll -= wUncommon) < 0f) bucket = EnchantmentRewardRarity.Uncommon;
		else if ((roll -= wCurse) < 0f) bucket = EnchantmentRewardRarity.Curse;
		else if ((roll -= wRare) < 0f) bucket = EnchantmentRewardRarity.Rare;
		else bucket = EnchantmentRewardRarity.Special;

		return byRarity.TryGetValue(bucket, out var picked) && picked.Count > 0
			? rng.NextItem(picked)
			: rng.NextItem(byRarity.Values.SelectMany(x => x).ToList());
	}

	private static decimal RollEnchantAmount(Rng rng, EnchantmentModel pick)
	{
		if (pick.ShowAmount)
			return rng.NextInt(1, 4);
		return 1m;
	}

	/// <summary>将首张可附魔的候选牌强制附上铃铛诅咒（含遗物发放），用于调试控制台。</summary>
	private static void TryApplyForcedBellCurseCardReward(Player player, List<CardCreationResult> results, Rng rng)
	{
		EnchantmentModel bellPick;
		try
		{
			bellPick = ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(typeof(BellCurseEnchantment)));
		}
		catch (ModelNotFoundException)
		{
			return;
		}

		foreach (var result in results)
		{
			var card = result.Card;
			if (card.Enchantment != null)
				continue;
			if (ShouldSkipEnchantingRewardCard(card))
				continue;
			if (!bellPick.CanEnchant(card))
				continue;

			var enchant = (EnchantmentModel)bellPick.MutableClone();
			var amount = RollEnchantAmount(rng, bellPick);
			CardCmd.Enchant(enchant, card, amount);
			if (enchant is BellCurseEnchantment)
				BellCurseReward.MarkPendingRelicGrant(card);
			return;
		}
	}
}
