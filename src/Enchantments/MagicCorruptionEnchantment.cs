using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Powers;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>
/// 魔法腐化：此牌耗能固定为 3；打出后施加 <see cref="MagicCorruptionPower"/>，自身打出后从战斗中移除（去向 None）。
/// 能力效果：使你其他带附魔的牌能量与辉星费用视为 0，打出后消耗（能力牌等保持原去向）。
/// </summary>
public sealed class MagicCorruptionEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int FixedEnergyCost = 3;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<MagicCorruptionPower>() };

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		Card.EnergyCost.SetCustomBaseCost(FixedEnergyCost);
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		if (!ReferenceEquals(card, Card))
			return (pileType, position);
		if (card.Type == CardType.Power || card.IsDupe)
			return (pileType, position);

		return (PileType.None, position);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await PowerCmd.Apply<MagicCorruptionPower>(player.Creature, 1m, player.Creature, Card);
	}
}
