using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.Patches;

/// <summary>
/// 本体 <c>CombatStateTracker</c> 延迟刷新里只对 <c>LocalContext.GetMe</c> 调用
/// <see cref="PlayerCombatState.RecalculateCardValues"/>；改费附魔在 <c>RecalculateValues</c> 中写
/// <c>SetCustomBaseCost</c>，队友牌在对方客户端不刷新会导致联机 checksum 分叉（GitHub #10）。
/// </summary>
[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.RecalculateCardValues))]
internal static class PlayerCombatStateRecalculateAllPlayersInMultiPatch
{
	private static bool _sweeping;

	[HarmonyPostfix]
	private static void Postfix(PlayerCombatState __instance)
	{
		if (RunManager.Instance?.NetService?.Type.IsMultiplayer() != true)
			return;

		if (_sweeping)
			return;

		CombatState? combatState = null;
		foreach (var card in __instance.AllCards)
		{
			combatState = card.CombatState;
			if (combatState != null)
				break;
		}

		if (combatState == null || combatState.Players.Count <= 1)
			return;

		_sweeping = true;
		try
		{
			foreach (var p in combatState.Players)
			{
				if (ReferenceEquals(p.PlayerCombatState, __instance))
					continue;
				p.PlayerCombatState?.RecalculateCardValues();
			}
		}
		finally
		{
			_sweeping = false;
		}
	}
}
