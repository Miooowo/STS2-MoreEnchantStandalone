using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>返回：打出后若将进入弃牌堆，则改为回到手牌（与粒子墙 <c>ParticleWall</c> 相同机制）。</summary>
public sealed class ReturnToHandEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		if (Card != card || pileType != PileType.Discard)
			return (pileType, position);
		return (PileType.Hand, position);
	}
}
