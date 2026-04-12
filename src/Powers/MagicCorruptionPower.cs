using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Powers;

/// <summary>
/// 魔法腐化（能力）：打出带「魔法腐化」附魔的牌后施加。使你其他带附魔的牌能量与辉星费用视为 0，打出后消耗（能力牌等保持原去向）。
/// </summary>
public sealed class MagicCorruptionPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

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
