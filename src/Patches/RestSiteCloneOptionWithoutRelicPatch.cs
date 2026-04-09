using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 允许玩家在休息点使用「克隆」选项：只要牌组中存在带 <see cref="Clone"/> 附魔的牌，
/// 就添加 <see cref="CloneRestSiteOption"/>，不再依赖遗物（如 PaelsGrowth）提供该选项。
/// </summary>
[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
internal static class RestSiteCloneOptionWithoutRelicPatch
{
	[HarmonyPostfix]
	private static void Postfix(Player player, ref List<RestSiteOption> __result)
	{
		if (player?.Deck?.Cards == null || __result == null)
			return;

		// 已有克隆选项则不重复添加（例如仍持有 PaelsGrowth 时）。
		if (__result.Any(o => o is CloneRestSiteOption))
			return;

		var cloneId = ModelDb.GetId<Clone>();
		bool hasCloneCard = player.Deck.Cards.Any(c => c?.Enchantment != null && (c.Enchantment is Clone || c.Enchantment.Id == cloneId));
		if (!hasCloneCard)
			return;

		__result.Add(new CloneRestSiteOption(player));
	}
}

