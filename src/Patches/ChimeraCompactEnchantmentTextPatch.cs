using HarmonyLib;
using MoreEnchant;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 精小：无辉星机制的牌不展示含 <c>{Stars:starIcons()}</c> 的句子（GitHub #9），与 <see cref="CardEnchantEligibility.CardUsesStarCost"/> 一致。
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.DynamicDescription), MethodType.Getter)]
internal static class ChimeraCompactEnchantmentDynamicDescriptionPatch
{
	[HarmonyPostfix]
	private static void Postfix(EnchantmentModel __instance, ref LocString __result)
	{
		if (__instance is not ChimeraCompactEnchantment c || c.Card == null)
			return;
		if (CardEnchantEligibility.CardUsesStarCost(c.Card))
			return;

		var description = new LocString("enchantments", "CHIMERA_COMPACT_ENCHANTMENT.description_noStars");
		description.Add("Amount", c.Amount);
		var dynamicVarSet = c.DynamicVars.Clone(c);
		dynamicVarSet.ClearPreview();
		c.Card.UpdateDynamicVarPreview(CardPreviewMode.None, null, dynamicVarSet);
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(c));
		dynamicVarSet.AddTo(description);
		__result = description;
	}
}

[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.DynamicExtraCardText), MethodType.Getter)]
internal static class ChimeraCompactEnchantmentDynamicExtraCardTextPatch
{
	[HarmonyPostfix]
	private static void Postfix(EnchantmentModel __instance, ref LocString? __result)
	{
		if (__result == null)
			return;
		if (__instance is not ChimeraCompactEnchantment c || c.Card == null)
			return;
		if (CardEnchantEligibility.CardUsesStarCost(c.Card))
			return;

		var extra = new LocString("enchantments", "CHIMERA_COMPACT_ENCHANTMENT.extraCardText_noStars");
		extra.Add("Amount", c.Amount);
		c.DynamicVars.AddTo(extra);
		__result = extra;
	}
}
