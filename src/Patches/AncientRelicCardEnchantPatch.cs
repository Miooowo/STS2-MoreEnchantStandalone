using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MoreEnchant.Patches;

/// <summary>
/// 先古之民遗物生成/变化出的 Ancient 卡：不走 <see cref="MegaCrit.Sts2.Core.Factories.CardFactory.CreateForReward"/>，
/// 需在遗物路径上补随机附魔（且 Ancient 不出诅咒档，逻辑由 <see cref="MoreEnchantCardRewardUtil"/> 处理）。
/// </summary>
internal static class AncientRelicCardEnchantPatch
{
	/// <summary>
	/// 古老牙齿的变牌实际由 <see cref="CardCmd.Transform(CardModel, CardModel, CardPreviewStyle)"/> 完成；
	/// 为避免 Transform 内部重建实例导致附魔丢失，这里对“最终入堆的那张牌”做补附魔。
	/// </summary>
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
	private static bool ArchaicToothAfterObtainedPrefix(ArchaicTooth __instance, ref Task __result)
	{
		__result = RunArchaicTooth(__instance);
		return false;
	}

	private static async Task RunArchaicTooth(ArchaicTooth tooth)
	{
		var owner = AccessTools.PropertyGetter(typeof(RelicModel), "Owner")?.Invoke(tooth, null) as Player;
		if (owner == null)
		{
			Log.Warn("[MoreEnchant] ArchaicTooth AfterObtained: owner null, skipping.");
			return;
		}

		// 原版逻辑：用私有方法获取可升格的起始牌
		var starter = AccessTools.Method(typeof(ArchaicTooth), "GetTranscendenceStarterCard")
			?.Invoke(tooth, new object[] { owner }) as CardModel;
		if (starter == null)
		{
			Log.Warn("[MoreEnchant] ArchaicTooth AfterObtained: no starter card found, skipping.");
			return;
		}

		// 通过原版私有方法生成 replacement（会处理升级与“继承起始附魔”）
		var replacement = AccessTools.Method(typeof(ArchaicTooth), "GetTranscendenceTransformedCard")?.Invoke(tooth, new object[] { starter }) as CardModel;
		if (replacement == null)
		{
			Log.Warn("[MoreEnchant] ArchaicTooth AfterObtained: replacement null, skipping.");
			return;
		}

		// Transform 会返回最终加入牌堆的那张牌
		var res = await CardCmd.Transform(starter, replacement, CardPreviewStyle.HorizontalLayout);
		var added = res?.cardAdded;
		if (added == null)
		{
			Log.Warn("[MoreEnchant] ArchaicTooth AfterObtained: transform produced null cardAdded, skipping.");
			return;
		}

		// 若原版已继承起始牌附魔，则不再叠加随机附魔（单卡仅允许一个附魔）
		if (added.Enchantment != null)
		{
			Log.Info($"[MoreEnchant] ArchaicTooth AfterObtained: transformed card already enchanted ({added.Enchantment.Id.Entry}), skipping random enchant.");
			return;
		}

		// Ancient 牌：按 Ancient 独立开关/概率附魔，且不会出现诅咒档
		if (added.Rarity != CardRarity.Ancient)
		{
			Log.Info($"[MoreEnchant] ArchaicTooth AfterObtained: transformed card rarity {added.Rarity}, skipping random enchant.");
			return;
		}

		var list = new List<CardCreationResult> { new CardCreationResult(added) };
		var options = CardCreationOptions.ForNonCombatWithDefaultOdds(new[] { owner.Character.CardPool });
		Log.Info("[MoreEnchant] ArchaicTooth AfterObtained: attempting random enchant for Ancient transformed card.");
		MoreEnchantCardRewardUtil.ApplyRandomEnchantments(owner, list, options);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.AfterObtained))]
	private static bool DustyTomeAfterObtainedPrefix(DustyTome __instance, ref Task __result)
	{
		__result = Run(__instance);
		return false;
	}

	private static async Task Run(DustyTome tome)
	{
		var owner = AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Models.RelicModel), "Owner")?.Invoke(tome, null) as Player;
		if (owner == null)
			return;

		var id = tome.AncientCard;
		if (id == null)
			return;

		var card = owner.RunState.CreateCard(SaveUtil.CardOrDeprecated(id), owner);
		CardCmd.Upgrade(card);

		if (card.Enchantment == null)
		{
			var list = new List<CardCreationResult> { new CardCreationResult(card) };
			var options = CardCreationOptions.ForNonCombatWithDefaultOdds(
				new[] { owner.Character.CardPool });
			MoreEnchantCardRewardUtil.ApplyRandomEnchantments(owner, list, options);
			card = list[0].Card;
		}

		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f, CardPreviewStyle.HorizontalLayout);
	}
}

