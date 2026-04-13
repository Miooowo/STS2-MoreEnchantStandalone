using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MoreEnchant;

namespace MoreEnchant.Patches;

/// <summary>
/// 变牌核心入口：<see cref="CardTransformation.GetReplacement"/> 在工厂或预构建替牌返回后统一处理；
/// 比仅补丁 <see cref="MegaCrit.Sts2.Core.Factories.CardFactory.CreateRandomCardForTransform"/> 更可靠（含潘多拉等预生成替牌）。
/// 原牌无附魔时再按奖励概率尝试随机附魔（树叶药膏等）。
/// </summary>
[HarmonyPatch(typeof(CardTransformation), nameof(CardTransformation.GetReplacement))]
internal static class CardTransformationGetReplacementPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardTransformation __instance, Rng? rng, ref CardModel __result)
	{
		if (__result == null)
			return;

		var original = __instance.Original;
		ModEnchantmentTransferUtil.CopyEnchantmentToIfMissing(original, __result);

		if (__result.Enchantment != null)
			return;
		if (original.Owner is not Player player)
			return;
		if (__result.Owner != player)
			return;

		MoreEnchantCardRewardUtil.TryApplyRandomEnchantAfterTransformCard(player, __result);
	}
}
