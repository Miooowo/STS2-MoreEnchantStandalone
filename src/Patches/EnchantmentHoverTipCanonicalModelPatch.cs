using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Patches;

/// <summary>原版 <see cref="EnchantmentModel.HoverTip"/> 未设置 <see cref="HoverTip.CanonicalModel"/>，无法在悬停 UI 中识别附魔；此处补上以便稀有度等补丁使用。</summary>
[HarmonyPatch(typeof(EnchantmentModel), MethodType.Getter)]
[HarmonyPatch("HoverTip")]
internal static class EnchantmentHoverTipCanonicalModelPatch
{
	[HarmonyPostfix]
	private static void Postfix(EnchantmentModel __instance, ref HoverTip __result)
	{
		EnchantmentModel? canonical = __instance.CanonicalInstance;
		if (canonical == null)
		{
			return;
		}
		__result.SetCanonicalModel(canonical);
	}
}
