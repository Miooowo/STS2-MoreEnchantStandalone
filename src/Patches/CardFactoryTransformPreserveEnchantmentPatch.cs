using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Random;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// <see cref="CardFactory.CreateRandomCardForTransform"/> 生成的新卡不带原牌附魔；先古遗物（如涅奥的树叶药膏）等变牌后应继承。
/// </summary>
internal static class CardFactoryTransformPreserveEnchantmentPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(
		typeof(CardFactory),
		nameof(CardFactory.CreateRandomCardForTransform),
		[typeof(CardModel), typeof(bool), typeof(Rng)])]
	private static void PostfixThreeArgs(CardModel original, CardModel __result)
	{
		TryCopyEnchantment(original, __result);
	}

	[HarmonyPostfix]
	[HarmonyPatch(
		typeof(CardFactory),
		nameof(CardFactory.CreateRandomCardForTransform),
		[typeof(CardModel), typeof(IEnumerable<CardModel>), typeof(bool), typeof(Rng)])]
	private static void PostfixFourArgs(CardModel original, CardModel __result)
	{
		TryCopyEnchantment(original, __result);
	}

	private static void TryCopyEnchantment(CardModel? original, CardModel? result)
	{
		if (original?.Enchantment == null || result == null || result.Enchantment != null)
			return;

		var enchCopy = (EnchantmentModel)original.Enchantment.ClonePreservingMutability();
		result.EnchantInternal(enchCopy, enchCopy.Amount);
		result.Enchantment!.ModifyCard();
		result.FinalizeUpgradeInternal();

		if (result.Enchantment is BellCurseEnchantment bell)
			bell.ResetRewardRelicGrantGateForClonedCard();
	}
}
