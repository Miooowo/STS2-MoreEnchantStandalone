using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace MoreEnchant.Patches;

/// <summary>
/// 容错：部分异常战斗收尾路径可能把 encounterId/monsterId 写成 null，
/// 原版在记录失败统计时会直接抛 ArgumentNullException。
/// 这里仅做空值保护，避免跑局结算崩溃。
/// </summary>
[HarmonyPatch]
internal static class ProgressSaveManagerNullEncounterGuardPatch
{
	[HarmonyPatch(typeof(ProgressSaveManager), "IncrementEncounterLoss")]
	[HarmonyPrefix]
	private static bool SkipNullEncounterId(ModelId encounterId)
	{
		return encounterId != null;
	}

	[HarmonyPatch(typeof(ProgressSaveManager), "IncrementEnemyFightLoss")]
	[HarmonyPrefix]
	private static bool SkipNullMonsterId(ModelId monster)
	{
		return monster != null;
	}
}
