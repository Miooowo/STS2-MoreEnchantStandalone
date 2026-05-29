using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.Powers;

namespace MoreEnchant.Patches;

/// <summary>
/// 任意本体死亡后，立即清理对应灵魂，避免“本体已死但灵魂残留到下个回合”。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
internal static class SoulDetachmentCleanupOnDeathPatch
{
	[HarmonyPostfix]
	private static void Postfix(
		IRunState runState,
		CombatState? combatState,
		Creature creature,
		bool wasRemovalPrevented,
		float deathAnimLength,
		ref Task __result)
	{
		__result = RunAfterHook(__result, combatState, creature);
	}

	private static async Task RunAfterHook(Task original, CombatState? combatState, Creature creature)
	{
		await original;
		if (combatState == null || !creature.IsEnemy)
			return;

		// 跑局失败结算阶段不再额外追加击杀，避免干扰收尾。
		if (combatState.Players.All(p => !p.Creature.IsAlive))
			return;

		foreach (var enemy in combatState.Enemies.ToList())
		{
			if (!enemy.IsAlive)
				continue;

			var link = enemy.GetPower<SoulDetachmentLinkPower>();
			if (link?.Target == creature)
				await CreatureCmd.Kill(enemy, force: true);
		}
	}
}
