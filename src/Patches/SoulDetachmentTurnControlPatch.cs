using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MoreEnchant.Powers;

namespace MoreEnchant.Patches;

/// <summary>
/// 回合末控制：灵魂维持晕眩，且当本体死亡时清理灵魂。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnEnd))]
internal static class SoulDetachmentTurnControlPatch
{
	[HarmonyPostfix]
	private static void Postfix(
		ICombatState combatState,
		CombatSide side,
		IEnumerable<Creature> creatures,
		ref Task __result)
	{
		__result = RunAfterHook(__result, combatState, side);
	}

	private static async Task RunAfterHook(Task original, ICombatState combatState, CombatSide side)
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

			// 分段/替身类敌人在某些阶段切换时可能不是 IsDead=true，
			// 但会表现为不再存活或已脱离当前战斗状态；这些情况同样应清理灵魂。
			var body = link.Target;
			if (body == null || !body.IsAlive || body.CombatState != combatState)
			{
				await CreatureCmd.Kill(enemy, force: true);
				continue;
			}

			// 在敌方回合结束后补晕，可确保下一轮始终展示并执行“晕眩意图”。
			await CreatureCmd.Stun(enemy);
		}
	}
}
