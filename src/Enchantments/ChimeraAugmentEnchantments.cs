using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>移植自 ChimeraTheSpire 的卡牌修饰符（作为附魔实现）。</summary>
internal static class ChimeraAugmentEnchantments
{
	internal static bool IsMoveDamage(ValueProp props) => props.HasFlag(ValueProp.Move);
}

/// <summary>打击：<see cref="CardTag.Strike"/> + 攻击伤害 +6；标题显示打击后缀。</summary>
public sealed class ChimeraStrikeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal MoveDamageBonus = 6m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	/// <summary>勿使用 <see cref="DamageVar"/>：其 <c>UpdateCardPreview</c> 会再调用
	/// <see cref="EnchantmentModel.EnchantDamageAdditive"/>，与固定加伤叠加会显示/结算成双倍。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DynamicVar("StrikeDmg", MoveDamageBonus); }
	}

	protected override void OnEnchant()
	{
		CardStrikeTagUtil.ApplyStrikeTag(Card);
	}

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
	{
		if (!ChimeraAugmentEnchantments.IsMoveDamage(props))
			return 0m;
		return MoveDamageBonus;
	}
}

/// <summary>宝石：获得 +2 重放。</summary>
public sealed class ChimeraGemEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Special;

	public override bool HasExtraCardText => false;

	public override int EnchantPlayCount(int originalPlayCount) =>
		originalPlayCount + 2;
}

/// <summary>启动：固有。</summary>
public sealed class ChimeraInnateEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => false;

	protected override void OnEnchant()
	{
		if (Card != null)
		{
			CardCmd.ApplyKeyword(Card, CardKeyword.Innate);
		}
	}
}

/// <summary>精小：伤害与格挡约为 2/3。（不处理费用变化）</summary>
public sealed class ChimeraCompactEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override void RecalculateValues()
	{
		// Chimera：耗能 -1（X 费不处理）
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(System.Math.Max(0, canonical - 1));
	}

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) =>
		ChimeraAugmentEnchantments.IsMoveDamage(props) ? (2m / 3m) : 1m;

	public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) =>
		props.HasFlag(ValueProp.Move) ? (2m / 3m) : 1m;
}

/// <summary>笨重：伤害与格挡按 (费用+1)/费用 放大。（不处理费用变化）</summary>
public sealed class ChimeraBulkyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int CostIncrease = 1;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(CostIncrease); }
	}

	public override void RecalculateValues()
	{
		// Chimera：耗能 +1（X 费不处理）
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + CostIncrease);
	}

	private decimal Ratio()
	{
		int c = Card?.EnergyCost.Canonical ?? 0;
		if (c < 1) return 1m;
		return (decimal)(c + 1) / c;
	}

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) =>
		ChimeraAugmentEnchantments.IsMoveDamage(props) ? Ratio() : 1m;

	public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) =>
		props.HasFlag(ValueProp.Move) ? Ratio() : 1m;
}

/// <summary>超巨化：伤害变为 3 倍。</summary>
public sealed class ChimeraGiganticEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageOrHpLoss(card);

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) =>
		ChimeraAugmentEnchantments.IsMoveDamage(props) ? 3m : 1m;
}

/// <summary>固化：格挡变为 3 倍。</summary>
public sealed class ChimeraSolidifyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveBlockNumbers(card);

	public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) =>
		props.HasFlag(ValueProp.Move) ? 3m : 1m;
}

/// <summary>重刃：力量对该牌攻击的加成按 3 倍计算（额外 +2×力量）。</summary>
public sealed class ChimeraHeavyBladeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
	{
		if (!ValuePropUtil.IsPoweredAttack(props))
			return 0m;

		int strength = Card?.Owner?.Creature.GetPowerAmount<StrengthPower>() ?? 0;
		if (strength == 0)
			return 0m;

		const int mult = 3;
		return (mult - 1) * strength;
	}
}

/// <summary>开悟：打出后本回合将手牌费用降低至 1（回合结束恢复）。</summary>
public sealed class ChimeraEnlightenmentEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.PlayerCombatState?.Hand == null)
			return;

		foreach (var c in Card.Owner.PlayerCombatState.Hand.Cards)
		{
			if (!c.EnergyCost.CostsX)
			{
				c.EnergyCost.SetThisTurn(1, reduceOnly: true);
				c.DynamicVars.RecalculateForUpgradeOrEnchant();
			}
		}

		await Task.CompletedTask;
	}
}

/// <summary>巩固：打出后将你的格挡翻倍。</summary>
public sealed class ChimeraEntrenchEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var creature = Card?.Owner?.Creature;
		if (creature == null || creature.Block <= 0)
			return;

		await CreatureCmd.GainBlock(creature, creature.Block, ValueProp.Move, cardPlay, fast: false);
	}
}

