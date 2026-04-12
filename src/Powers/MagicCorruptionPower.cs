using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Powers;

/// <summary>
/// 魔法腐化（能力）：打出带「魔法腐化」附魔的牌后施加。使你其他带附魔的牌能量与辉星费用视为 0，打出后消耗（能力牌等保持原去向）；并为所有带附魔的牌添加[消耗]关键词。
/// </summary>
public sealed class MagicCorruptionPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	/// <summary>供文案 <c>{Energy:energyIcons()}</c> 解析（附魔牌能量费用视为 0 的展示）。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(0); }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

	/// <summary>能力生效瞬间：为当前战斗中所有带附魔的牌补上[消耗]关键词。</summary>
	internal static void ApplyExhaustKeywordToAllEnchantedCards(Player player)
	{
		if (player.PlayerCombatState == null)
			return;
		foreach (var pile in player.PlayerCombatState.AllPiles)
		{
			foreach (var card in pile.Cards)
				TryApplyExhaustKeywordToCard(card);
		}
	}

	private static void TryApplyExhaustKeywordToCard(CardModel card)
	{
		if (card.Enchantment == null)
			return;
		if (card.Keywords.Contains(CardKeyword.Exhaust))
			return;
		CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
	{
		if (!addedByPlayer || card.Enchantment == null)
			return Task.CompletedTask;
		if (card.Owner is not Player player || player.Creature != Owner)
			return Task.CompletedTask;
		TryApplyExhaustKeywordToCard(card);
		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (card.Owner.Creature != Owner || originalCost < 0m)
			return false;
		if (card.EnergyCost.CostsX)
			return false;
		if (card.Enchantment == null)
			return false;
		if (card.Enchantment is MagicCorruptionEnchantment)
			return false;

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (card.Owner.Creature != Owner || originalCost < 0m)
			return false;
		if (card.Enchantment == null)
			return false;
		if (card.Enchantment is MagicCorruptionEnchantment)
			return false;
		if (card.HasStarCostX)
			return false;

		modifiedCost = 0m;
		return true;
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		if (card.Owner.Creature != Owner)
			return (pileType, position);
		if (card.Enchantment == null)
			return (pileType, position);
		if (card.Enchantment is MagicCorruptionEnchantment)
			return (pileType, position);
		if (card.Type == CardType.Power || card.IsDupe)
			return (pileType, position);
		if (pileType == PileType.None)
			return (pileType, position);

		return (PileType.Exhaust, position);
	}
}
