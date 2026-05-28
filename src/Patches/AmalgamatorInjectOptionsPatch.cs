using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 为熔合者事件追加“注入打击 / 注入防御”两个事件型附魔选项。
/// </summary>
[HarmonyPatch(typeof(Amalgamator), "GenerateInitialOptions")]
internal static class AmalgamatorInjectOptionsPatch
{
	private const string InjectStrikesOptionKey = "AMALGAMATOR.pages.INITIAL.options.INJECT_STRIKES";
	private const string InjectDefendsOptionKey = "AMALGAMATOR.pages.INITIAL.options.INJECT_DEFENDS";
	private const string InjectStrikesResultKey = "AMALGAMATOR.pages.INJECT_STRIKES.description";
	private const string InjectDefendsResultKey = "AMALGAMATOR.pages.INJECT_DEFENDS.description";

	[HarmonyPostfix]
	private static void Postfix(Amalgamator __instance, ref IReadOnlyList<EventOption> __result)
	{
		var options = __result.ToList();
		if (!options.Any(o => o.TextKey == InjectStrikesOptionKey))
		{
			options.Add(new EventOption(
				__instance,
				() => InjectStrikes(__instance),
				InjectStrikesOptionKey,
				HoverTipFactory.FromEnchantment<UltimateStrikeEnchantment>()));
		}

		if (!options.Any(o => o.TextKey == InjectDefendsOptionKey))
		{
			options.Add(new EventOption(
				__instance,
				() => InjectDefends(__instance),
				InjectDefendsOptionKey,
				HoverTipFactory.FromEnchantment<UltimateDefendEnchantment>()));
		}

		__result = options;
	}

	private static async Task InjectStrikes(Amalgamator eventModel)
	{
		var owner = eventModel.Owner;
		if (owner == null)
			return;

		var removed = (await CardSelectCmd.FromDeckForRemoval(
			owner,
			new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2),
			card => IsRemovableBasicTagged(card, CardTag.Strike))).ToList();
		await CardPileCmd.RemoveFromDeck(removed);

		var template = ModelDb.Enchantment<UltimateStrikeEnchantment>();
		var target = (await CardSelectCmd.FromDeckForEnchantment(
			owner,
			template,
			1,
			card => card != null && template.CanEnchant(card) && card.Type == CardType.Attack,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
		if (target != null)
			CardCmd.Enchant((EnchantmentModel)template.MutableClone(), target, 1m);

		TrySetEventFinished(eventModel, InjectStrikesResultKey);
	}

	private static async Task InjectDefends(Amalgamator eventModel)
	{
		var owner = eventModel.Owner;
		if (owner == null)
			return;

		var removed = (await CardSelectCmd.FromDeckForRemoval(
			owner,
			new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2),
			card => IsRemovableBasicTagged(card, CardTag.Defend))).ToList();
		await CardPileCmd.RemoveFromDeck(removed);

		var template = ModelDb.Enchantment<UltimateDefendEnchantment>();
		var target = (await CardSelectCmd.FromDeckForEnchantment(
			owner,
			template,
			1,
			card => card != null && template.CanEnchant(card) && card.Type == CardType.Skill &&
			        CardEnchantEligibility.CardHasMoveBlockNumbers(card),
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
		if (target != null)
			CardCmd.Enchant((EnchantmentModel)template.MutableClone(), target, 1m);

		TrySetEventFinished(eventModel, InjectDefendsResultKey);
	}

	private static bool IsRemovableBasicTagged(CardModel card, CardTag tag) =>
		card.Tags.Contains(tag) &&
		card.Rarity == CardRarity.Basic &&
		card.IsRemovable;

	private static void TrySetEventFinished(EventModel eventModel, string descKey)
	{
		try
		{
			var method = AccessTools.Method(typeof(EventModel), "SetEventFinished");
			method?.Invoke(eventModel, new object[] { new LocString("events", descKey) });
		}
		catch
		{
			// ignore
		}
	}
}
