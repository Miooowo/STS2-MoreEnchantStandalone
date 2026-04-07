using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 在玩家从奖励中真正获得卡牌后发放铃铛诅咒遗物；避免在 <c>CreateForReward</c> 同步 postfix 里异步发遗物导致不触发或上下文错误。
/// </summary>
[HarmonyPatch(typeof(RewardSynchronizer), nameof(RewardSynchronizer.SyncLocalObtainedCard))]
internal static class RewardSynchronizerBellCurseRelicGrantPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel card)
	{
		if (card?.Owner is not Player p)
			return;
		if (!BellCurseReward.TryConsumePendingRelicGrant(card))
			return;
		_ = TaskHelper.RunSafely(BellCurseReward.GrantCore(p));
	}
}
