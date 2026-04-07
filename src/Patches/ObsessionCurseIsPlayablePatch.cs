using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>执迷：手牌中存在执迷诅咒时，只能打出带执迷附魔的牌，直到将其打出或移出手牌。</summary>
[HarmonyPatch(typeof(CardModel), MethodType.Getter)]
[HarmonyPatch("IsPlayable")]
internal static class ObsessionCurseIsPlayablePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (!__result)
			return;

		var owner = __instance.Owner;
		if (owner is not Player p)
			return;

		var hand = p.PlayerCombatState?.Hand.Cards;
		if (hand == null)
			return;

		var hasObsession = false;
		foreach (var c in hand)
		{
			if (c?.Enchantment is ObsessionCurseEnchantment)
			{
				hasObsession = true;
				break;
			}
		}

		if (!hasObsession)
			return;

		if (__instance.Enchantment is ObsessionCurseEnchantment)
			return;

		__result = false;
	}
}
