using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>事件专属附魔标记：不会进入随机附魔池。</summary>
public interface IEventExclusiveEnchantment;

/// <summary>究极打击：攻击获得 +14 打出伤害。</summary>
public sealed class UltimateStrikeEnchantment : ModEnchantmentTemplate, IEventExclusiveEnchantment
{
	private const decimal DamageBonus = 14m;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		// 不使用 DamageVar，避免预览路径重复叠加 EnchantDamageAdditive 导致紫字翻倍。
		get { yield return new DynamicVar("UltimateStrikeDamage", DamageBonus); }
	}

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) &&
		card.Type == CardType.Attack &&
		CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props) =>
		props.HasFlag(ValueProp.Move) ? DamageBonus : 0m;
}

/// <summary>究极防御：带有打出格挡的技能牌获得 +11 格挡。</summary>
public sealed class UltimateDefendEnchantment : ModEnchantmentTemplate, IEventExclusiveEnchantment
{
	private const decimal BlockBonus = 11m;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DynamicVar("UltimateDefendBlock", BlockBonus); }
	}

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) &&
		card.Type == CardType.Skill &&
		CardEnchantEligibility.CardHasMoveBlockNumbers(card);

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature == null)
			return;
		await CreatureCmd.GainBlock(Card.Owner.Creature, BlockBonus, ValueProp.Move, cardPlay, fast: false);
	}
}
