using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MoreEnchant;

/// <summary>根据卡牌 <see cref="CardModel.DynamicVars"/> 判断是否带有「打出时」的伤害/格挡数值，用于奖励附魔过滤。</summary>
internal static class CardEnchantEligibility
{
	internal static bool CardHasMoveDamageNumbers(CardModel card)
	{
		var dv = card.DynamicVars;
		if (dv.TryGetValue("Damage", out var d0) && d0 is DamageVar d &&
		    d.Props.HasFlag(ValueProp.Move) && d.BaseValue > 0)
			return true;
		if (dv.TryGetValue("OstyDamage", out var o0) && o0 is OstyDamageVar od &&
		    od.Props.HasFlag(ValueProp.Move) && od.BaseValue > 0)
			return true;
		if (dv.TryGetValue("ExtraDamage", out var e0) && e0 is ExtraDamageVar ed && ed.BaseValue > 0)
			return true;
		if (dv.TryGetValue("CalculatedDamage", out var c0) && c0 is CalculatedDamageVar cd &&
		    cd.Props.HasFlag(ValueProp.Move))
		{
			if (cd.BaseValue > 0)
				return true;
			if (dv.TryGetValue("CalculationExtra", out var xe) && xe.BaseValue > 0)
				return true;
		}

		return false;
	}

	/// <summary>超巨化等仅放大「打出伤害」的附魔：需牌面有移动伤害，或打出时失去生命（<see cref="HpLossVar"/>）。</summary>
	internal static bool CardHasMoveDamageOrHpLoss(CardModel card)
	{
		if (CardHasMoveDamageNumbers(card))
			return true;
		if (!card.DynamicVars.TryGetValue("HpLoss", out var v) || v is not HpLossVar hl)
			return false;
		return hl.BaseValue > 0;
	}

	internal static bool CardHasMoveBlockNumbers(CardModel card)
	{
		var dv = card.DynamicVars;
		if (dv.TryGetValue("Block", out var b0) && b0 is BlockVar bv &&
		    bv.Props.HasFlag(ValueProp.Move) && bv.BaseValue > 0)
			return true;
		if (dv.TryGetValue("CalculatedBlock", out var c0) && c0 is CalculatedBlockVar cb &&
		    cb.Props.HasFlag(ValueProp.Move))
		{
			if (cb.BaseValue > 0)
				return true;
			if (dv.TryGetValue("CalculationExtra", out var xe) && xe.BaseValue > 0)
				return true;
		}

		return false;
	}

	/// <summary>幽灵：攻击牌仅当有打出格挡（Move）；技能/能力牌当有牌面格挡数值（含 Unpowered 的 <see cref="BlockVar"/>，如创世之柱、寿衣）、或 <see cref="PowerVar{T}"/>。</summary>
	internal static bool CardEligibleForSpectralGhost(CardModel card)
	{
		if (card.Type == CardType.Attack)
			return CardHasMoveBlockNumbers(card);
		if (card.Type is CardType.Skill or CardType.Power)
			return CardHasPowerVar(card) || CardHasPositiveBlockDynamicVarsForSpectral(card);
		return false;
	}

	/// <summary>牌面带正数格挡变量（Move 或 Unpowered 均可）。能力牌常用 Unpowered <see cref="BlockVar"/> 表示传给能力的数值，不含 Move。</summary>
	internal static bool CardHasPositiveBlockDynamicVarsForSpectral(CardModel card)
	{
		var dv = card.DynamicVars;
		if (dv.TryGetValue("Block", out var b0) && b0 is BlockVar bv && bv.BaseValue > 0m)
			return true;
		if (dv.TryGetValue("CalculatedBlock", out var c0) && c0 is CalculatedBlockVar cb)
		{
			if (cb.BaseValue > 0m)
				return true;
			if (dv.TryGetValue("CalculationExtra", out var xe) && xe.BaseValue > 0m)
				return true;
		}

		return false;
	}

	internal static bool CardHasPowerVar(CardModel card)
	{
		foreach (var v in card.DynamicVars.Values)
		{
			var t = v.GetType();
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PowerVar<>))
				return true;
		}

		return false;
	}
}
