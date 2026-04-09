using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MoreEnchant.Patches;

/// <summary>
/// 原版用 <c>c.Enchantment is Clone</c> 筛选可克隆牌；奖励等方式附上克隆后，
/// 在部分环境下实例可能无法通过该类型判断。改为以 <see cref="ModelDb.GetId{T}"/> 识别「克隆」附魔。
/// </summary>
[HarmonyPatch(typeof(CloneRestSiteOption), nameof(CloneRestSiteOption.OnSelect))]
internal static class CloneRestSiteOptionCompatPatch
{
	private static readonly System.Reflection.FieldInfo? OwnerField =
		AccessTools.Field(typeof(RestSiteOption), "<Owner>k__BackingField") ??
		AccessTools.Field(typeof(RestSiteOption), "Owner");

	[HarmonyPrefix]
	private static bool Prefix(CloneRestSiteOption __instance, ref Task<bool> __result)
	{
		if (OwnerField?.GetValue(__instance) is not Player owner)
		{
			__result = Task.FromResult(false);
			return false;
		}

		__result = RunCloneAsync(owner);
		return false;
	}

	private static async Task<bool> RunCloneAsync(Player owner)
	{
		var cloneId = ModelDb.GetId<Clone>();
		var toClone = owner.Deck.Cards.Where(c =>
			c.Enchantment != null && (c.Enchantment is Clone || c.Enchantment.Id == cloneId)).ToList();

		var results = new List<CardPileAddResult>();
		foreach (var item in toClone)
		{
			var card = owner.RunState.CloneCard(item);
			results.Add(await CardPileCmd.Add(card, PileType.Deck));
		}

		CardCmd.PreviewCardPileAdd(results, 1.2f, CardPreviewStyle.MessyLayout);
		return true;
	}
}
