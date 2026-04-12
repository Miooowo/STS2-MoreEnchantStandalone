using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 部分卡牌在 <see cref="CardModel.UpgradeInternal"/> 的 <c>OnUpgrade</c> 中会 <see cref="CardModel.RemoveKeyword"/>(<see cref="CardKeyword.Exhaust"/>)，
/// 导致抢救附魔在 <see cref="RescueEnchantment.OnEnchant"/> 中施加的消耗被卸除。升级流程结束后若仍带抢救则补回消耗。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FinalizeUpgradeInternal))]
internal static class RescueEnchantmentExhaustAfterUpgradePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance)
	{
		if (__instance.Enchantment is not RescueEnchantment)
			return;
		if (__instance.Keywords.Contains(CardKeyword.Exhaust))
			return;
		CardCmd.ApplyKeyword(__instance, CardKeyword.Exhaust);
	}
}
