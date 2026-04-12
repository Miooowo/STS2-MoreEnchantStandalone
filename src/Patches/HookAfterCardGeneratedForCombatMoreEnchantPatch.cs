using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Patches;

/// <summary>
/// 战斗内生成并入堆的牌在 <see cref="Hook.AfterCardGeneratedForCombat"/> 触发时尝试随机附魔
/// （与 CardPileCmd.AddGeneratedCardsToCombat、CardCmd 变换等原版入口一致）。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardGeneratedForCombat))]
internal static class HookAfterCardGeneratedForCombatMoreEnchantPatch
{
	[HarmonyPrefix]
	private static void Prefix(CombatState combatState, CardModel card, bool addedByPlayer)
	{
		_ = combatState;
		MoreEnchantCardRewardUtil.TryApplyRandomEnchantToCombatGeneratedCard(card, addedByPlayer);
	}
}
