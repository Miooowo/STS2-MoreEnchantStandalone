using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>灼热：允许额外升级档位；第一次显示为「牌名+」，第二次起为「牌名+等级」（与原版多段升级规则衔接）。</summary>
[HarmonyPatch(typeof(CardModel), "get_MaxUpgradeLevel")]
internal static class CardModelScorchingMaxUpgradePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref int __result)
	{
		if (__instance.Enchantment is ScorchingEnchantment)
			__result = System.Math.Max(__result, 999999999);
	}
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
internal static class CardModelScorchingTitlePatch
{
	/// <summary>先于 <see cref="CardModelStrikeTitlePatch"/>，把 +1 规范为单独的 +。</summary>
	[HarmonyPostfix]
	[HarmonyPriority(Priority.First)]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (__instance.Enchantment is not ScorchingEnchantment)
			return;
		if (!__instance.IsUpgraded)
			return;

		var baseTitle = __instance.TitleLocString.GetFormattedText();
		if (__instance.CurrentUpgradeLevel <= 1)
			__result = baseTitle + "+";
		else
			__result = $"{baseTitle}+{__instance.CurrentUpgradeLevel}";
	}
}
