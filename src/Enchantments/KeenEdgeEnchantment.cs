using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>锐锋：攻击牌力量加成的伤害额外 +1（可叠层）。</summary>
public sealed class KeenEdgeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool ShowAmount => true;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
	{
		if (!ValuePropUtil.IsPoweredAttack(props))
			return 0m;
		return base.Amount;
	}
}
