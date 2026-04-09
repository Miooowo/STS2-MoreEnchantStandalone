using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Enchantments;

/// <summary>凡庸：本回合在持有该诅咒的手牌中已打出的牌数（用于 <see cref="Patches.MediocreCurseIsPlayablePatch"/>）。</summary>
internal static class MediocreCursePlayLimiter
{
	private static readonly Dictionary<Player, int> PlaysByPlayer = new(new ReferenceEqualityComparer());

	internal static void OnPlayerTurnStart(Player player)
	{
		if (player == null)
			return;
		PlaysByPlayer[player] = 0;
	}

	internal static void RecordPlay(Player? owner, CardPlay cardPlay)
	{
		if (owner == null)
			return;
		// 联机：仅统计「本玩家」打出的牌；队友出牌不应累加到持有凡庸者的计数上。
		if (!ReferenceEquals(cardPlay.Card?.Owner, owner))
			return;
		var hand = owner.PlayerCombatState?.Hand.Cards;
		if (hand == null)
			return;

		bool mediocreInHand = hand.Any(c => c?.Enchantment is MediocreCurseEnchantment);
		bool playedMediocre = cardPlay.Card?.Enchantment is MediocreCurseEnchantment;
		if (!mediocreInHand && !playedMediocre)
			return;

		PlaysByPlayer.TryGetValue(owner, out var n);
		PlaysByPlayer[owner] = n + 1;
	}

	internal static bool BlocksFurtherPlays(Player player)
	{
		if (player?.PlayerCombatState?.Hand.Cards == null)
			return false;

		bool hasMediocre = false;
		foreach (var c in player.PlayerCombatState.Hand.Cards)
		{
			if (c?.Enchantment is MediocreCurseEnchantment)
			{
				hasMediocre = true;
				break;
			}
		}

		if (!hasMediocre)
			return false;

		PlaysByPlayer.TryGetValue(player, out var played);
		return played >= 3;
	}

	private sealed class ReferenceEqualityComparer : IEqualityComparer<Player>
	{
		public bool Equals(Player? x, Player? y) => ReferenceEquals(x, y);
		public int GetHashCode(Player obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
	}
}

