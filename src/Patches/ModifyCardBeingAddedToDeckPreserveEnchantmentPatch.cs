using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant;

namespace MoreEnchant.Patches;

/// <summary>
/// 熔火蛋/冻结蛋等遗物在卡牌加入牌组时会 <see cref="MegaCrit.Sts2.Core.Runs.RunState.CloneCard"/> 再升级，
/// 返回的新实例可能丢失已在 <see cref="CardFactoryTransformPreserveEnchantmentPatch"/> 等处写上的附魔；此处从调用时的入参卡补回。
/// 直加牌组且无附魔时再按 <see cref="MoreEnchantSettings.DeckDirectEnchantChancePercent"/> 随机附魔（涅奥苦痛、扭蛋、卷轴箱等）。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardBeingAddedToDeck))]
internal static class ModifyCardBeingAddedToDeckPreserveEnchantmentPatch
{
	[HarmonyPostfix]
	private static void Postfix([HarmonyArgument(1)] CardModel card, ref CardModel __result)
	{
		ModEnchantmentTransferUtil.CopyEnchantmentToIfMissing(card, __result);
		if (__result?.Owner is Player p)
			MoreEnchantCardRewardUtil.TryApplyRandomEnchantDirectDeckAdd(p, __result);
	}
}
