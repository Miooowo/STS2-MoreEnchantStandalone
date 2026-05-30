using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
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
	private static void Prefix(object[] __args)
	{
		if (__args == null || __args.Length < 2 || __args[1] is not CardModel card)
			return;

		var creator = ResolveCreator(__args);
		// 不同端 Hook 参数在某些时点可能出现 creator/flag 可见性差异；
		// 以 card.Owner / creator 任一可用作为兜底，避免一端进入一端不进入导致 RNG 与卡状态分叉。
		var addedByPlayer = ResolveAddedByPlayer(__args) || card.Owner is Player || creator != null;
		MoreEnchantCardRewardUtil.TryApplyRandomEnchantToCombatGeneratedCard(card, addedByPlayer, creator);
	}

	private static bool ResolveAddedByPlayer(object[] args)
	{
		if (args.Length < 3)
			return false;

		return args[2] switch
		{
			bool b => b,
			// 新版 Hook 参数为 creator(Player)；只要存在创建者即视为玩家生成。
			// 不能用 ReferenceEquals(card.Owner, creator) 判定，联机跨端对象引用不稳定会导致一端 true 一端 false，引发 RNG 分叉。
			Player creator => creator != null,
			_ => false,
		};
	}

	private static Player? ResolveCreator(object[] args)
	{
		if (args.Length < 3)
			return null;
		return args[2] as Player;
	}
}
