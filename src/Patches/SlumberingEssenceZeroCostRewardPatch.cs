using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 沉眠精华：降低未打出牌的费用；对已是 0 能（非 X）的牌无意义，奖励池不应附魔。
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant), MethodType.Normal)]
internal static class SlumberingEssenceZeroCostRewardPatch
{
	[HarmonyPostfix]
	private static void Postfix(EnchantmentModel __instance, CardModel card, ref bool __result)
	{
		if (!__result || __instance is not SlumberingEssence)
			return;
		if (card.EnergyCost.CostsX)
			return;
		if (card.EnergyCost.Canonical <= 0)
			__result = false;
	}
}
