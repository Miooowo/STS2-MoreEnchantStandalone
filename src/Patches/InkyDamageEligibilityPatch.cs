using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 墨影（Inky）应只附到具备打出伤害数值的牌，避免随机附魔命中无伤害牌。
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant), MethodType.Normal)]
internal static class InkyDamageEligibilityPatch
{
	[HarmonyPostfix]
	private static void Postfix(EnchantmentModel __instance, CardModel card, ref bool __result)
	{
		if (!__result || __instance is not Inky)
			return;

		if (!CardEnchantEligibility.CardHasMoveDamageNumbers(card))
			__result = false;
	}
}
