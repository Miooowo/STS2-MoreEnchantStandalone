using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Patches;

/// <summary>设置面板打开时重试注册 RitsuLib 设置页（处理延迟加载场景）。</summary>
[HarmonyPatch(typeof(NSettingsPanel), nameof(NSettingsPanel._Ready))]
internal static class MoreEnchantGeneralSettingsPanelPatch
{
	[HarmonyPostfix]
	private static void Postfix(NSettingsPanel __instance)
	{
		_ = RitsuLibModSettingsCompat.TryRegisterSettingsPage();
	}
}
