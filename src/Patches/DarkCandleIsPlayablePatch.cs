using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>幽暗烛火：允许打出原本不可打出的诅咒牌（放开 IsPlayable）。</summary>
[HarmonyPatch(typeof(CardModel), MethodType.Getter)]
[HarmonyPatch("IsPlayable")]
internal static class DarkCandleIsPlayablePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (__result)
			return;
		if (__instance.Enchantment is not DarkCandleEnchantment)
			return;
		if (__instance.Owner == null)
			return;

		__result = true;
	}
}
