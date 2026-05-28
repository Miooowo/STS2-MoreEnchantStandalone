using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

	public override bool HasExtraCardText => false;

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
	private bool _previewFaceBlockApplied;

	public override bool HasExtraCardText => false;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DynamicVar("UltimateDefendBlock", BlockBonus); }
	}

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) &&
		card.Type == CardType.Skill &&
		CardEnchantEligibility.CardHasMoveBlockNumbers(card);

	protected override void OnEnchant()
	{
		base.OnEnchant();
		if (Card == null)
			return;

		// 显式刷新：在“附魔落到牌上”的当帧就把卡面格挡数字与紫字同步出来。
		RecalculateValues();
		Card.DynamicVars.RecalculateForUpgradeOrEnchant();
		ApplyPreviewFaceBlockBumpIfNeeded();
	}

	public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource,
		CardPlay? cardPlay) =>
		props.IsPoweredCardOrMonsterMoveBlock() ? BlockBonus : 0m;

	private void ApplyPreviewFaceBlockBumpIfNeeded()
	{
		if (Card == null || !Card.IsEnchantmentPreview || _previewFaceBlockApplied)
			return;

		// 事件附魔预览卡常不走完整战斗数值钩子；这里仅抬高 PreviewValue（不改 BaseValue），
		// 以保留绿色差值高亮（类似究极打击）。
		if (Card.DynamicVars.TryGetValue("Block", out var blockVar))
			blockVar.PreviewValue = blockVar.BaseValue + BlockBonus;
		if (Card.DynamicVars.TryGetValue("CalculatedBlock", out var calculatedBlockVar))
			calculatedBlockVar.PreviewValue = calculatedBlockVar.BaseValue + BlockBonus;

		_previewFaceBlockApplied = true;
	}
}
