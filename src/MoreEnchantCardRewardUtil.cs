using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Enchantments.Mocks;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.Enchantments;
using MoreEnchant.Enchantments.Beta;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant;

internal static class MoreEnchantCardRewardUtil
{
	private static readonly string[] SlipperyWoodenBridgeEventIdCandidates =
	[
		"SLIPPERY_WOODEN_BRIDGE",
		"SLIPPERY_BRIDGE",
		"WOODEN_BRIDGE",
	];
	private static readonly string[] SlipperyWoodenBridgeEventTitleCandidates =
	[
		"滑脚木桥",
		"Slippery Wooden Bridge",
	];

	public static void ApplyRandomEnchantments(Player player, List<CardCreationResult> results,
		CardCreationOptions options)
	{
		if (options.Flags.HasFlag(CardCreationFlags.NoModifyHooks))
			return;
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();

		var rng = options.Source == CardCreationSource.Shop
			? player.PlayerRng.Shops
			: player.PlayerRng.Rewards;
		var templates = GetEligibleRewardTemplates(settings);

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

		if (MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardHextechForge &&
		    options.Source == CardCreationSource.Encounter)
		{
			MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardHextechForge = false;
			TryApplyForcedHextechForgeCardReward(results, rng);
		}

		if (MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardHateBridge &&
		    options.Source == CardCreationSource.Encounter)
		{
			MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardHateBridge = false;
			TryApplyForcedHateBridgeCardReward(results, rng);
		}

		if (MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardRandomEnchantOnPickup &&
		    options.Source == CardCreationSource.Encounter)
		{
			MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardRandomEnchantOnPickup = false;
			TryApplyForcedRandomEnchantOnPickupCardReward(results, rng);
		}

		foreach (var result in results)
		{
			var card = result.Card;
			if (card.Enchantment != null)
				continue;
			if (ShouldSkipEnchantingRewardCard(card))
				continue;

			bool ancientCard = card.Rarity == CardRarity.Ancient;
			int chancePercent;
			if (options.Source == CardCreationSource.Shop)
			{
				if (!settings.ShopEnchantEnabled)
					continue;
				chancePercent = settings.ShopEnchantChancePercent;
			}
			else if (ancientCard)
			{
				if (!settings.AncientRewardEnchantEnabled)
					continue;
				chancePercent = settings.AncientRewardEnchantChancePercent;
			}
			else
			{
				chancePercent = settings.RewardEnchantChancePercent;
			}

			if (chancePercent <= 0 || rng.NextInt(0, 100) >= chancePercent)
				continue;

			var pick = RollEnchantmentTemplate(
				card,
				templates,
				rng,
				settings,
				excludeCurse: ancientCard,
				options.Source);
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
	internal static void TryApplyRandomEnchantToCombatGeneratedCard(CardModel card, bool addedByPlayer, Player? creator = null)
	{
		if (!addedByPlayer || card == null)
			return;
		var player = creator ?? card.Owner as Player;
		if (player == null)
			return;
		// 战斗内生牌附魔同样属于确定性流程：联机下各端必须一致执行，
		// 否则会出现卡牌附魔状态不一致并导致 Transformations RNG 计数分叉。

		if (card.Enchantment != null)
			return;
		if (ShouldSkipEnchantingRewardCard(card))
			return;
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		if (!settings.CombatGeneratedEnchantEnabled)
			return;

		var chancePercent = settings.CombatGeneratedEnchantChancePercent;
		if (chancePercent <= 0)
			return;

		var rng = player.PlayerRng.Transformations;
		if (rng.NextInt(0, 100) >= chancePercent)
			return;

		var templates = GetEligibleRewardTemplates(settings);
		var ancient = card.Rarity == CardRarity.Ancient;
		var excludeCurse = ancient;
		var pick = RollEnchantmentTemplate(
			card,
			templates,
			rng,
			settings,
			excludeCurse,
			CardCreationSource.Encounter);
		if (pick == null)
			return;

		var enchant = (EnchantmentModel)pick.MutableClone();
		var amount = RollEnchantAmount(rng, pick);
		CardCmd.Enchant(enchant, card, amount);
	}

	/// <summary>
	/// 变牌替牌在继承原牌附魔后仍无附魔时（例如原牌无附魔），按 <see cref="MoreEnchantSettings.TransformEnchantChancePercent"/> 等尝试随机附魔。
	/// </summary>
	internal static void TryApplyRandomEnchantAfterTransformCard(Player player, CardModel card)
	{
		if (card.Enchantment != null)
			return;
		// 变牌结果是确定性流程，联机下必须各端一致消耗 Transformations RNG，
		// 不能按 LocalContext.IsMine 仅本机执行，否则会导致 checksum 分叉。
		if (ShouldSkipEnchantingRewardCard(card))
			return;
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		if (!settings.TransformEnchantEnabled)
			return;

		var chancePercent = settings.TransformEnchantChancePercent;
		if (chancePercent <= 0)
			return;

		var rng = player.PlayerRng.Transformations;
		if (rng.NextInt(0, 100) >= chancePercent)
			return;

		var templates = GetEligibleRewardTemplates(settings);
		var ancient = card.Rarity == CardRarity.Ancient;
		var pick = RollEnchantmentTemplate(
			card,
			templates,
			rng,
			settings,
			excludeCurse: ancient,
			CardCreationSource.Encounter);
		if (pick == null)
			return;

		var enchant = (EnchantmentModel)pick.MutableClone();
		var amount = RollEnchantAmount(rng, pick);
		CardCmd.Enchant(enchant, card, amount);
	}

	/// <summary>
	/// 非 <see cref="CardFactory.CreateForReward"/> 路径、直接入牌组的牌（巨大扭蛋额外打击/防御、涅奥的苦痛、卷轴箱等）；
	/// 与 <see cref="MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromChooseABundleScreen"/> 卷轴箱预览共用 <see cref="MoreEnchantSettings.DeckDirectEnchantChancePercent"/>。
	/// </summary>
	internal static void TryApplyRandomEnchantDirectDeckAdd(Player player, CardModel card)
	{
		if (card.Enchantment != null)
			return;
		// 直加牌组（事件/遗物/卷轴箱等）同样会进入联机校验；
		// 必须各端一致执行，避免仅本机消耗 Rewards RNG 造成状态分歧。
		if (ShouldSkipEnchantingRewardCard(card))
			return;
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		if (!settings.DeckDirectEnchantEnabled)
			return;

		var chancePercent = settings.DeckDirectEnchantChancePercent;
		if (chancePercent <= 0)
			return;

		var rng = player.PlayerRng.Rewards;
		if (rng.NextInt(0, 100) >= chancePercent)
			return;

		var templates = GetEligibleRewardTemplates(settings);
		var ancient = card.Rarity == CardRarity.Ancient;
		var pick = RollEnchantmentTemplate(
			card,
			templates,
			rng,
			settings,
			excludeCurse: ancient,
			CardCreationSource.Encounter);
		if (pick == null)
			return;

		var enchant = (EnchantmentModel)pick.MutableClone();
		var amount = RollEnchantAmount(rng, pick);
		CardCmd.Enchant(enchant, card, amount);
	}

	/// <summary>
	/// 新局创建时对初始卡组逐张尝试随机附魔；默认关闭，联机客机遵循房主设置。
	/// </summary>
	internal static void TryApplyRandomEnchantToStartingDeck(Player player)
	{
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		if (!settings.StartingDeckEnchantEnabled)
			return;

		var chancePercent = settings.StartingDeckEnchantChancePercent;
		if (chancePercent <= 0)
			return;

		var cards = player.Deck.Cards;
		if (cards.Count == 0)
			return;

		var rng = player.PlayerRng.Rewards;
		var templates = GetEligibleRewardTemplates(settings);
		foreach (var card in cards)
		{
			if (card == null || card.Enchantment != null)
				continue;
			if (ShouldSkipEnchantingRewardCard(card))
				continue;
			if (rng.NextInt(0, 100) >= chancePercent)
				continue;

			var pick = RollEnchantmentTemplate(
				card,
				templates,
				rng,
				settings,
				excludeCurse: card.Rarity == CardRarity.Ancient,
				CardCreationSource.Encounter);
			if (pick == null)
				continue;

			var enchant = (EnchantmentModel)pick.MutableClone();
			var amount = RollEnchantAmount(rng, pick);
			CardCmd.Enchant(enchant, card, amount);
		}
	}

	/// <summary>在卡牌真正入组后处理“拾起触发”类附魔（铃铛诅咒/锻造器等）。</summary>
	internal static void TryHandleOnCardPickedUp(Player player, CardModel card)
	{
		if (card?.Enchantment is BellCurseEnchantment bell && bell.TryTakeRewardRelicGrantOnce())
			_ = TaskHelper.RunSafely(BellCurseReward.GrantCoreAfterUiFrame(player));

		if (card?.Enchantment is HextechForgeEnchantment forge && forge.TryTakePickupGrant(player))
			_ = TaskHelper.RunSafely(HextechRunesCompat.TryGrantRandomForgeAfterUiFrame(player));

		if (card?.Enchantment is RandomEnchantOnPickupEnchantment randomEnchant && randomEnchant.TryTakePickupTriggerOnce())
			_ = TaskHelper.RunSafely(TryApplyRandomEnchantmentToSelectedDeckCardAfterUiFrame(player, card));

		if (card?.Enchantment is HateBridgeEnchantment hateBridge && hateBridge.TryTakePickupTriggerOnce())
			TryEnterSlipperyWoodenBridgeEvent(player);
	}

	private static void TryEnterSlipperyWoodenBridgeEvent(Player player)
	{
		var runManager = RunManager.Instance;
		if (player?.RunState == null || runManager == null)
			return;
		if (runManager.NetService?.Type.IsMultiplayer() == true)
			return;

		var targetEvent = FindSlipperyWoodenBridgeEvent();
		if (targetEvent == null)
			return;

		TryEnterEventRoomViaDebugApi(runManager, targetEvent);
	}

	private static EventModel? FindSlipperyWoodenBridgeEvent()
	{
		foreach (var e in ModelDb.AllEvents)
		{
			if (e == null)
				continue;
			if (SlipperyWoodenBridgeEventIdCandidates.Any(id =>
				    string.Equals(e.Id.Entry, id, StringComparison.OrdinalIgnoreCase)))
				return e;
		}

		foreach (var e in ModelDb.AllEvents)
		{
			if (e == null)
				continue;

			try
			{
				var title = e.Title.GetFormattedText();
				if (SlipperyWoodenBridgeEventTitleCandidates.Any(t =>
					    title.Contains(t, StringComparison.OrdinalIgnoreCase)))
					return e;
			}
			catch (LocException)
			{
				// ignore broken loc entries
			}
		}

		return null;
	}

	private static void TryEnterEventRoomViaDebugApi(RunManager runManager, EventModel eventModel)
	{
		try
		{
			var method = runManager.GetType().GetMethod(
				"EnterRoomDebug",
				BindingFlags.Public | BindingFlags.Instance);
			if (method == null)
				return;

			var parameters = method.GetParameters();
			if (parameters.Length != 4)
				return;

			var roomTypeArg = ParseEnum(parameters[0].ParameterType, "Event", "Unknown");
			var mapPointTypeArg = ParseEnum(parameters[1].ParameterType, "Event", "Unknown");
			if (roomTypeArg == null || mapPointTypeArg == null)
				return;

			method.Invoke(runManager, [roomTypeArg, mapPointTypeArg, eventModel, false]);
		}
		catch
		{
			// ignore; pickup triggers should never hard-crash reward flow
		}
	}

	private static object? ParseEnum(Type enumType, params string[] preferredNames)
	{
		if (!enumType.IsEnum)
			return null;

		foreach (var name in preferredNames)
		{
			if (Enum.TryParse(enumType, name, ignoreCase: true, out var parsed) && parsed != null)
				return parsed;
		}

		var values = Enum.GetValues(enumType);
		return values.Length > 0 ? values.GetValue(0) : null;
	}

	private static async Task TryApplyRandomEnchantmentToSelectedDeckCardAfterUiFrame(Player player, CardModel sourceCard)
	{
		await Task.Yield();

		var candidates = player.Deck.Cards
			.Where(c => c != null && !ReferenceEquals(c, sourceCard) && c.Enchantment == null)
			.ToList();
		if (candidates.Count == 0)
			return;

		var selected = (await CardSelectCmd.FromSimpleGrid(
				new BlockingPlayerChoiceContext(),
				candidates,
				player,
				new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)))
			.FirstOrDefault();
		if (selected == null)
			return;

		var rng = player.PlayerRng.Rewards;

		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		var templates = GetEligibleRewardTemplates(settings);
		var pick = RollEnchantmentTemplate(
			selected,
			templates,
			rng,
			settings,
			excludeCurse: selected.Rarity == CardRarity.Ancient,
			CardCreationSource.Encounter);
		if (pick == null)
			return;

		var enchant = (EnchantmentModel)pick.MutableClone();
		var amount = RollEnchantAmount(rng, pick);
		CardCmd.Enchant(enchant, selected, amount);
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
		if (template is Clone or TezcatarasEmber or Goopy or Glam or Imbued or Instinct)
			return EnchantmentRewardRarity.Special;
		if (template is IRewardEnchantRarity withRarity)
			return withRarity.RewardRarity;
		return EnchantmentRewardRarity.Common;
	}

	private static bool IsEligibleRewardTemplate(EnchantmentModel t) =>
		t is not DeprecatedEnchantment and not MockFreeEnchantment and not IEventExclusiveEnchantment;

	/// <summary>设置页黑名单候选：与随机池资格一致（不含 Beta / 白黑名单过滤）。</summary>
	internal static IEnumerable<EnchantmentModel> EnumerateSettingsPoolCandidates() =>
		ModelDb.DebugEnchantments.Where(IsEligibleRewardTemplate);

	private static EnchantmentModel[] GetEligibleRewardTemplates(MoreEnchantSettings settings)
	{
		return ModelDb.DebugEnchantments
			.Where(IsEligibleRewardTemplate)
			.Where(t => settings.BetaRewardEnchantmentsEnabled || t is not IBetaGatedRewardEnchantment)
			.Where(t => EnchantmentPoolFilter.IsAllowed(t, settings))
			.ToArray();
	}

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
		bool excludeCurse,
		CardCreationSource source)
	{
		var byRarity = new Dictionary<EnchantmentRewardRarity, List<EnchantmentModel>>();
		foreach (var t in templates)
		{
			if (!IsTemplateAllowedForSource(t, source))
				continue;
			if (!t.CanEnchant(card))
				continue;

			var r = GetRewardRarity(t);
			if (excludeCurse && r == EnchantmentRewardRarity.Curse)
				continue;
			if (r == EnchantmentRewardRarity.Hidden)
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

	private static bool IsTemplateAllowedForSource(EnchantmentModel template, CardCreationSource source)
	{
		if (template is RoyallyApproved && source != CardCreationSource.Shop)
			return false;
		return true;
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
		var settings = MoreEnchantMultiplayerSettings.GetEffectiveSettings();
		var templates = GetEligibleRewardTemplates(settings);

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

	/// <summary>将首张可附魔的候选牌强制附上锻造器附魔（仅海克斯符文联动可用时生效）。</summary>
	private static void TryApplyForcedHextechForgeCardReward(List<CardCreationResult> results, Rng rng)
	{
		EnchantmentModel forgePick;
		try
		{
			forgePick = ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(typeof(HextechForgeEnchantment)));
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
			if (!forgePick.CanEnchant(card))
				continue;

			var enchant = (EnchantmentModel)forgePick.MutableClone();
			var amount = RollEnchantAmount(rng, forgePick);
			CardCmd.Enchant(enchant, card, amount);
			return;
		}
	}

	/// <summary>将首张可附魔候选牌强制附上「我恨桥」。</summary>
	private static void TryApplyForcedHateBridgeCardReward(List<CardCreationResult> results, Rng rng)
	{
		EnchantmentModel bridgePick;
		try
		{
			bridgePick = ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(typeof(HateBridgeEnchantment)));
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
			if (!bridgePick.CanEnchant(card))
				continue;

			var enchant = (EnchantmentModel)bridgePick.MutableClone();
			var amount = RollEnchantAmount(rng, bridgePick);
			CardCmd.Enchant(enchant, card, amount);
			return;
		}
	}

	/// <summary>将首张可附魔候选牌强制附上「随机附魔」。</summary>
	private static void TryApplyForcedRandomEnchantOnPickupCardReward(List<CardCreationResult> results, Rng rng)
	{
		EnchantmentModel randomEnchantPick;
		try
		{
			randomEnchantPick = ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(typeof(RandomEnchantOnPickupEnchantment)));
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
			if (!randomEnchantPick.CanEnchant(card))
				continue;

			var enchant = (EnchantmentModel)randomEnchantPick.MutableClone();
			var amount = RollEnchantAmount(rng, randomEnchantPick);
			CardCmd.Enchant(enchant, card, amount);
			return;
		}
	}
}
