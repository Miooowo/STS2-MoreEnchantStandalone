using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Powers;

namespace MoreEnchant.Patches;

/// <summary>
/// 灵魂链接（目标）展示“施加者名称”而非目标自身名称。
/// </summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SmartDescription), MethodType.Getter)]
internal static class SoulLinkTargetPowerDescriptionPatch
{
	[HarmonyPostfix]
	private static void Postfix(PowerModel __instance, ref LocString __result)
	{
		if (__instance is not MoreEnchantSoulLinkTargetPower targetPower)
			return;

		var sourceName = targetPower.GetLinkSourceName();
		if (string.IsNullOrWhiteSpace(sourceName))
			return;

		__result.Add("LinkSourceName", sourceName);
	}
}
