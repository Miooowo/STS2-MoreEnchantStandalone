using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.Patches;

/// <summary>
///     与私有重载 <c>CreateForReward(Player, IEnumerable&lt;CardModel&gt;, ...)</c> 区分，必须写明参数类型。
/// </summary>
[HarmonyPatch(
	typeof(CardFactory),
	nameof(CardFactory.CreateForReward),
	[typeof(Player), typeof(int), typeof(CardCreationOptions)])]
internal static class CardFactoryMoreEnchantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Player player, int cardCount, CardCreationOptions options,
		ref IEnumerable<CardCreationResult> __result)
	{
		if (player == null || __result == null)
			return;
		if (__result is not List<CardCreationResult> list)
			list = __result.ToList();

		MoreEnchantCardRewardUtil.ApplyRandomEnchantments(player, list, options);

		__result = list;
	}
}
