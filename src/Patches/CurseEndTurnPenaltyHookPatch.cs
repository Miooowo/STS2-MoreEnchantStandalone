using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 104+ 下部分附魔不再覆写 TurnEnd 钩子，改为在 Hook.BeforeTurnEnd 后补执行诅咒回合末自伤逻辑。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeTurnEnd))]
internal static class CurseEndTurnPenaltyHookPatch
{
	[HarmonyPostfix]
	private static void Postfix(CombatState combatState, CombatSide side, ref Task __result)
	{
		__result = RunAfterHook(__result, combatState, side);
	}

	private static async Task RunAfterHook(Task original, CombatState combatState, CombatSide side)
	{
		await original;
		if (side != CombatSide.Player)
			return;

		var localNetId = LocalContext.NetId;
		if (!localNetId.HasValue)
			return;

		foreach (var player in combatState.Players)
		{
			var hand = player.PlayerCombatState?.Hand.Cards;
			if (hand == null || hand.Count == 0)
				continue;

			foreach (var card in hand)
			{
				if (card.Enchantment is not EnchantmentModel enchantment)
					continue;

				var ctx = new HookPlayerChoiceContext(player, localNetId.Value, GameActionType.Combat);
				switch (enchantment)
				{
					case BadLuckCurseEnchantment badLuck:
						await badLuck.ApplyTurnEndPenalty(ctx, side);
						break;
					case DecayCurseEnchantment decay:
						await decay.ApplyTurnEndPenalty(ctx, side);
						break;
					case RegretCurseEnchantment regret:
						await regret.ApplyTurnEndPenalty(ctx, side);
						break;
				}
			}
		}
	}
}
