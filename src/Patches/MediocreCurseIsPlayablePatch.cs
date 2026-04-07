using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>凡庸：手牌中有该诅咒时，本回合已打出 ≥3 张牌则禁止再打出任何牌。</summary>
[HarmonyPatch(typeof(CardModel), MethodType.Getter)]
[HarmonyPatch("IsPlayable")]
internal static class MediocreCurseIsPlayablePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (!__result)
			return;

		var owner = __instance.Owner;
		if (owner is not Player p)
			return;

		if (!MediocreCursePlayLimiter.BlocksFurtherPlays(p))
			return;

		__result = false;
	}
}
