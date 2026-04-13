using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MoreEnchant;

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
	private static void PostfixThreeArgs(CardModel original, bool isInCombat, Rng rng, ref CardModel __result)
	{
		ModEnchantmentTransferUtil.CopyEnchantmentToIfMissing(original, __result);
	}

	[HarmonyPostfix]
	[HarmonyPatch(
		typeof(CardFactory),
		nameof(CardFactory.CreateRandomCardForTransform),
		[typeof(CardModel), typeof(IEnumerable<CardModel>), typeof(bool), typeof(Rng)])]
	private static void PostfixFourArgs(
		CardModel original,
		IEnumerable<CardModel> options,
		bool isInCombat,
		Rng rng,
		ref CardModel __result)
	{
		ModEnchantmentTransferUtil.CopyEnchantmentToIfMissing(original, __result);
	}
}
