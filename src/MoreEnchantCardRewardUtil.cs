using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
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

		var rng = options.Source == CardCreationSource.Shop
			? player.PlayerRng.Shops
			: player.PlayerRng.Rewards;
		var templates = ModelDb.DebugEnchantments.Where(IsEligibleRewardTemplate).ToArray();
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();

		if (options.Source == CardCreationSource.Shop && !settings.ShopEnchantEnabled)
			return;

		if (MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardBellCurse &&
		    options.Source == CardCreationSource.Encounter)
		{
			MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardBellCurse = false;
			TryApplyForcedBellCurseCardReward(player, results, rng);
		}

		if (MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardRandomCurse &&
		    options.Source == CardCreationSource.Encounter)
		{
			MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardRandomCurse = false;
			TryApplyForcedRandomCurseCardReward(player, results, rng);
		}

		foreach (var result in results)
		{
			var card = result.Card;
			if (card.Enchantment != null)
				continue;
			if (ShouldSkipEnchantingRewardCard(card))
				continue;

			bool ancientCard = card.Rarity == CardRarity.Ancient;
			if (ancientCard && !settings.AncientRewardEnchantEnabled)
				continue;

			int chancePercent;
			if (options.Source == CardCreationSource.Shop)
				chancePercent = Math.Clamp(settings.ShopEnchantChancePercent, 0, 100);
			else if (ancientCard)
				chancePercent = Math.Clamp(settings.AncientRewardEnchantChancePercent, 0, 100);
			else
				chancePercent = Math.Clamp(settings.RewardEnchantChancePercent, 0, 100);

			if (chancePercent <= 0 || rng.NextInt(0, 100) >= chancePercent)
				continue;

			var pick = RollEnchantmentTemplate(card, templates, rng, settings, excludeCurse: ancientCard);
			if (pick == null)
				continue;

			var enchant = (EnchantmentModel)pick.MutableClone();
			var amount = RollEnchantAmount(rng, pick);

			CardCmd.Enchant(enchant, card, amount);
		}
	}

	/// <summary>
	/// 战斗内 <see cref="CardPileCmd.AddGeneratedCardsToCombat"/> 等路径生成并入库的牌；仅 <paramref name="addedByPlayer"/> 为真时处理。
	/// </summary>
	internal static void TryApplyRandomEnchantToCombatGeneratedCard(CardModel card, bool addedByPlayer)
	{
		if (!addedByPlayer || card?.Owner is not Player player)
			return;
		if (!LocalContext.IsMine(card))
			return;

		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		if (!settings.CombatGeneratedEnchantEnabled)
			return;
		if (card.Enchantment != null)
			return;
		if (ShouldSkipEnchantingRewardCard(card))
			return;

		var chancePercent = Math.Clamp(settings.CombatGeneratedEnchantChancePercent, 0, 100);
		if (chancePercent <= 0)
			return;

		var rng = player.PlayerRng.Transformations;
		if (rng.NextInt(0, 100) >= chancePercent)
			return;

		var templates = ModelDb.DebugEnchantments.Where(IsEligibleRewardTemplate).ToArray();
		var ancient = card.Rarity == CardRarity.Ancient;
		var excludeCurse = ancient;
		var pick = RollEnchantmentTemplate(card, templates, rng, settings, excludeCurse);
		if (pick == null)
			return;

		var enchant = (EnchantmentModel)pick.MutableClone();
		var amount = RollEnchantAmount(rng, pick);
		CardCmd.Enchant(enchant, card, amount);
	}

	/// <summary>
	/// 变牌替牌在继承原牌附魔后仍无附魔时（例如原牌无附魔），按非战斗奖励规则尝试随机附魔（与奖励牌同一套概率/设置）。
	/// </summary>
	internal static void TryApplyRandomEnchantAfterTransformCard(Player player, CardModel card)
	{
		if (card.Enchantment != null)
			return;
		if (!LocalContext.IsMine(card))
			return;
		if (ShouldSkipEnchantingRewardCard(card))
			return;

		var list = new List<CardCreationResult> { new CardCreationResult(card) };
		var options = CardCreationOptions.ForNonCombatWithDefaultOdds(new[] { player.Character.CardPool });
		ApplyRandomEnchantments(player, list, options);
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

	/// <summary>与 ChimeraTheSpire 奇美拉卡牌修饰的按卡牌稀有度分桶权重一致；诅咒档整体已下调。</summary>
	private static (float Common, float Uncommon, float Curse, float Rare, float Special) GetBucketWeightsForCardRarity(
		CardRarity rarity)
	{
		switch (rarity)
		{
			case CardRarity.Basic:
			case CardRarity.Common:
				return (0.50f, 0.30f, 0.10f, 0.079f, 0.001f);
			case CardRarity.Uncommon:
				return (0.30f, 0.40f, 0.05f, 0.14f, 0.01f);
			case CardRarity.Rare:
				return (0.25f, 0.30f, 0.01f, 0.05f, 0.05f);
			case CardRarity.Event:
			case CardRarity.Curse:
				return (0.3244f, 0.3333f, 0.12f, 0.10f, 0.0023f);
			case CardRarity.Ancient:
				return (0.20f, 0.20f, 0.00f, 0.35f, 0.25f);
			case CardRarity.Quest:
				return (0.20f, 0.30f, 0.10f, 0.05f, 0.10f);
			default:
				return (0.50f, 0.30f, 0.10f, 0.079f, 0.001f);
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

	/// <summary>被附魔的奖励牌越稀有，诅咒档权重越低（先古之民等路径可整档排除）。</summary>
	private static float CurseBucketScaleForRewardCard(CardRarity rarity) =>
		rarity switch
		{
			CardRarity.Basic or CardRarity.Common => 1f,
			CardRarity.Uncommon => 0.70f,
			CardRarity.Rare => 0.40f,
			CardRarity.Event or CardRarity.Curse => 0.55f,
			CardRarity.Ancient or CardRarity.Quest => 0.35f,
			_ => 0.80f,
		};

	private static EnchantmentModel? RollEnchantmentTemplate(CardModel card, EnchantmentModel[] templates,
		Rng rng,
		MoreEnchantSettings settings,
		bool excludeCurse)
	{
		var byRarity = new Dictionary<EnchantmentRewardRarity, List<EnchantmentModel>>();
		foreach (var t in templates)
		{
			if (!t.CanEnchant(card))
				continue;

			var r = GetRewardRarity(t);
			if (excludeCurse && r == EnchantmentRewardRarity.Curse)
				continue;
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
		if (!excludeCurse)
			wCurse *= 0.78f * CurseBucketScaleForRewardCard(card.Rarity);
		else
			wCurse = 0f;

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
			return;
		}
	}

	/// <summary>将首张可附魔的候选牌强制附上随机诅咒档附魔（铃铛诅咒遗物在拾起时由 RewardSynchronizer 补丁发放）。</summary>
	private static void TryApplyForcedRandomCurseCardReward(Player player, List<CardCreationResult> results, Rng rng)
	{
		var templates = ModelDb.DebugEnchantments.Where(IsEligibleRewardTemplate).ToArray();

		foreach (var result in results)
		{
			var card = result.Card;
			if (card.Enchantment != null)
				continue;
			if (ShouldSkipEnchantingRewardCard(card))
				continue;

			var curses = templates
				.Where(t => GetRewardRarity(t) == EnchantmentRewardRarity.Curse && t.CanEnchant(card))
				.ToList();
			if (curses.Count == 0)
				continue;

			var pick = rng.NextItem(curses);
			if (pick == null)
				continue;
			var enchant = (EnchantmentModel)pick.MutableClone();
			var amount = RollEnchantAmount(rng, pick);
			CardCmd.Enchant(enchant, card, amount);
			return;
		}
	}
}
