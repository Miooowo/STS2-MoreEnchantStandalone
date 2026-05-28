using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 蛇咬附魔要求基础耗能固定为 2；部分牌在升级时会改费，需在升级收尾后再矫正一次。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FinalizeUpgradeInternal))]
internal static class SnakebiteCostAfterUpgradePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance)
	{
		ApplySnakebiteUpgradeRules(__instance);
	}

	/// <summary>
	/// 升级预览通常会先走 UpgradeInternal（不一定完整走到 FinalizeUpgradeInternal）。
	/// 这里补一层，保证蛇咬在“查看升级”时也不会出现降费预览，并同步中毒层数预览（7 -> 10）。
	/// </summary>
	[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
	[HarmonyPostfix]
	private static void UpgradeInternalPostfix(CardModel __instance)
	{
		ApplySnakebiteUpgradeRules(__instance);
	}

	private static void ApplySnakebiteUpgradeRules(CardModel card)
	{
		if (card.Enchantment is not SnakebiteEnchantment snakebite)
			return;
		if (!card.EnergyCost.CostsX)
		{
			card.EnergyCost.SetCustomBaseCost(2);
			card.InvokeEnergyCostChanged();
		}

		// 预览与实际升级都强制刷新蛇咬动态值，确保显示基础7、升级10。
		snakebite.RecalculateValues();
	}
}
