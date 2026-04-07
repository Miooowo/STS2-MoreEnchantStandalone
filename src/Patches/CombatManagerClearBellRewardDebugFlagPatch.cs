using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MoreEnchant;

namespace MoreEnchant.Patches;

/// <summary>新战斗开始时清掉未消费的「强制铃铛奖励」调试请求，避免上一场未领奖励时污染下一场。</summary>
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.StartCombatInternal))]
internal static class CombatManagerClearBellRewardDebugFlagPatch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		MoreEnchantCombatRewardDebug.ForceNextEncounterCardRewardBellCurse = false;
	}
}
