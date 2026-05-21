using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.Patches;

/// <summary>
/// 新局创建完成（玩家种子已初始化）后，对所有玩家初始卡组尝试随机附魔。
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
internal static class RunStateStartingDeckEnchantPatch
{
	[HarmonyPostfix]
	private static void Postfix(RunState __result)
	{
		if (__result?.Players == null)
			return;

		foreach (var player in __result.Players)
		{
			if (player == null)
				continue;
			MoreEnchantCardRewardUtil.TryApplyRandomEnchantToStartingDeck(player);
		}
	}
}
