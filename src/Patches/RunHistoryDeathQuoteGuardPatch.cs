using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.Patches;

/// <summary>
/// 容错：战败结算时若历史数据出现空角色ID/空模型ID，原版获取死亡语录会抛异常并阻断 GameOver UI。
/// 这里兜底返回一条安全文案，避免界面崩溃。
/// </summary>
[HarmonyPatch(typeof(NRunHistory), nameof(NRunHistory.GetDeathQuote))]
internal static class RunHistoryDeathQuoteGuardPatch
{
	[HarmonyFinalizer]
	private static Exception? Finalizer(Exception? __exception, ref string __result, RunHistory history)
	{
		if (__exception == null)
			return null;

		Log.Warn($"[MoreEnchant] GetDeathQuote failed, fallback to safe quote: {__exception}");
		__result = BuildFallbackQuote(history);
		return null;
	}

	private static string BuildFallbackQuote(RunHistory history)
	{
		if (history.Win)
			return string.Empty;

		try
		{
			return new LocString("game_over_screen", "QUOTES.0").GetFormattedText();
		}
		catch
		{
			return string.Empty;
		}
	}
}
