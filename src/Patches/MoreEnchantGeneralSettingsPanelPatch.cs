using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MoreEnchant.Patches;

/// <summary>保留补丁入口，但不再注入任何 More Enchant 设置项。</summary>
[HarmonyPatch(typeof(NSettingsPanel), nameof(NSettingsPanel._Ready))]
internal static class MoreEnchantGeneralSettingsPanelPatch
{
	[HarmonyPostfix]
	private static void Postfix(NSettingsPanel __instance) { }
}
