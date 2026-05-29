using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using MoreEnchant.Powers;

namespace MoreEnchant.Patches;

/// <summary>
/// 回合末控制：灵魂维持晕眩，且当本体死亡时清理灵魂。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTurnEnd))]
internal static class SoulDetachmentTurnControlPatch
{
	[HarmonyPostfix]
	private static void Postfix(CombatState combatState, CombatSide side, ref Task __result)
	{
		__result = RunAfterHook(__result, combatState, side);
	}

	private static async Task RunAfterHook(Task original, CombatState combatState, CombatSide side)
	{
		await original;
		if (side != CombatSide.Enemy)
			return;

		// 若玩家已全部死亡（跑局正在失败结算），不要再追加任何战斗指令，避免污染结算流程。
		if (combatState.Players.All(p => !p.Creature.IsAlive))
			return;

		foreach (var enemy in combatState.Enemies.ToList())
		{
			if (!enemy.IsAlive)
				continue;

			var link = enemy.GetPower<SoulDetachmentLinkPower>();
			if (link == null)
				continue;

			// 本体已死亡时，灵魂应立即清理，避免残留在战场上。
			if (link.Target is { IsDead: true })
			{
				await CreatureCmd.Kill(enemy, force: true);
				continue;
			}

			// 在敌方回合结束后补晕，可确保下一轮始终展示并执行“晕眩意图”。
			await CreatureCmd.Stun(enemy);
		}
	}
}
