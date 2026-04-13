using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant;

namespace MoreEnchant.Patches;

/// <summary>
/// 卷轴箱 <see cref="MegaCrit.Sts2.Core.Models.Relics.ScrollBoxes"/> 在三选一预览展示前为每张候选牌掷骰附魔，与入组时 <see cref="ModifyCardBeingAddedToDeckPreserveEnchantmentPatch"/> 共用 <see cref="MoreEnchantSettings.DeckDirectEnchantChancePercent"/>。
/// </summary>
[HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromChooseABundleScreen))]
internal static class CardSelectCmdFromChooseABundleScreenPatch
{
	[HarmonyPrefix]
	private static void Prefix(Player player, IReadOnlyList<IReadOnlyList<CardModel>> bundles)
	{
		if (player == null || bundles == null)
			return;

		foreach (var bundle in bundles)
		{
			if (bundle == null)
				continue;
			foreach (var c in bundle)
			{
				if (c == null)
					continue;
				MoreEnchantCardRewardUtil.TryApplyRandomEnchantDirectDeckAdd(player, c);
			}
		}
	}
}
