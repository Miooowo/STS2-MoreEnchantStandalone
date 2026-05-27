using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>华丽（设计备注：礼帽人 / 华丽收场式）：仅战斗中且抽牌堆无牌时，伤害与格挡×3；满足时卡牌金边（同 <see cref="MegaCrit.Sts2.Core.Models.Cards.GrandFinale"/>）。
/// 伤害/格挡乘区仅作用于 <see cref="ValuePropUtil.IsPoweredAttack"/> / <see cref="ValuePropUtil.IsPoweredCardOrMonsterMoveBlock"/>，
/// 避免 <see cref="MegaCrit.Sts2.Core.Models.Cards.Omnislice"/> 等对溅射使用 <see cref="ValueProp.Unpowered"/> 时已含附魔加成的数值再次被×3。</summary>
public sealed class FinaleCurtainEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) &&
		(CardEnchantEligibility.CardHasMoveDamageOrHpLoss(card) ||
		 CardEnchantEligibility.CardHasMoveBlockNumbers(card));

	/// <summary>
	/// 非战斗、图鉴/奖励等预览下 <see cref="CardModel.CombatState"/> 为 null，不得用抽牌堆状态套用×3（否则会出现非战斗下数字被乘 3）。
	/// </summary>
	private bool FinaleMechanicActive
	{
		get
		{
			if (Card == null || !CombatManager.Instance.IsInProgress || Card.CombatState == null)
				return false;
			if (Card.Owner is not Player p)
				return false;
			return PileType.Draw.GetPile(p).Cards.Count == 0;
		}
	}

	public override bool ShouldGlowGold => FinaleMechanicActive;

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) =>
		ValuePropUtil.IsPoweredAttack(props) && FinaleMechanicActive ? 3m : 1m;

	public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) =>
		ValuePropUtil.IsPoweredCardOrMonsterMoveBlock(props) && FinaleMechanicActive ? 3m : 1m;
}
