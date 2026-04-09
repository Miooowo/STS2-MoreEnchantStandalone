using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.Patches;

/// <summary>商店卡牌由 <see cref="CardFactory.CreateForMerchant"/> 生成，不经 <see cref="CardFactory.CreateForReward"/>；此处补上随机附魔。</summary>
[HarmonyPatch(
	typeof(CardFactory),
	nameof(CardFactory.CreateForMerchant),
	[typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType)])]
internal static class CardFactoryMerchantMoreEnchantPatchCardType
{
	[HarmonyPostfix]
	private static void Postfix(Player player, ref CardCreationResult __result)
	{
		if (player == null || __result?.Card == null)
			return;

		var list = new List<CardCreationResult> { __result };
		var options = CardCreationOptions.ForRoom(player, RoomType.Shop);
		MoreEnchantCardRewardUtil.ApplyRandomEnchantments(player, list, options);
		__result = list[0];
	}
}

[HarmonyPatch(
	typeof(CardFactory),
	nameof(CardFactory.CreateForMerchant),
	[typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity)])]
internal static class CardFactoryMerchantMoreEnchantPatchCardRarity
{
	[HarmonyPostfix]
	private static void Postfix(Player player, ref CardCreationResult __result)
	{
		if (player == null || __result?.Card == null)
			return;

		var list = new List<CardCreationResult> { __result };
		var options = CardCreationOptions.ForRoom(player, RoomType.Shop);
		MoreEnchantCardRewardUtil.ApplyRandomEnchantments(player, list, options);
		__result = list[0];
	}
}
