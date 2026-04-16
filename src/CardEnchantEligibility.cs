using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MoreEnchant;

/// <summary>根据卡牌 <see cref="CardModel.DynamicVars"/> 判断是否带有「打出时」的伤害/格挡数值，用于奖励附魔过滤。</summary>
internal static class CardEnchantEligibility
{
	/// <summary>
	/// 灼热附魔按 <c>card.Id.Entry</c> 排除：单次降费至 0 后叠升无收益（破灭）、仅分支玩法无牌面数字（武装）、升级仅去消耗/虚无（恶魔护盾、回响形态）、仅加固有（杂耍）、衍生牌强化（隐秘匕首）等；数值模拟仍可能放行个别牌，故集中维护。
	/// </summary>
	private static readonly HashSet<string> ScorchingExcludedCardEntries = new(StringComparer.OrdinalIgnoreCase)
	{
		"HAVOC",
		"ARMAMENTS",
		"DEMONIC_SHIELD",
		"ECHO_FORM",
		"JUGGLING",
		"HIDDEN_DAGGERS",
	};

	internal static bool IsScorchingExcludedByCardId(CardModel card)
	{
		if (card?.Id.Entry is not { } entry)
			return false;
		return ScorchingExcludedCardEntries.Contains(entry);
	}

	/// <summary>
	/// 灼热：模拟下一次 <see cref="CardModel.UpgradeInternal"/> + <see cref="CardModel.FinalizeUpgradeInternal"/>，
	/// 若耗能（非 X）降低、星耗（非 X）降低、任一数理动态变量按「越大越好」变强，或 <c>HpLoss</c> 降低，则视为有牌面收益。
	/// 不可升级时由调用方另行处理；异常时保守返回 <c>true</c> 以免清空奖励池。
	/// </summary>
	internal static bool CardNextUpgradeImprovesFaceNumbers(CardModel card)
	{
		try
		{
			if (!card.IsUpgradable)
				return true;

			var probe = (CardModel)card.MutableClone();
			var energyCostsX = probe.EnergyCost.CostsX;
			var beforeEnergy = energyCostsX ? 0 : probe.EnergyCost.GetWithModifiers(CostModifiers.None);
			var hasStarCostX = probe.HasStarCostX;
			var beforeStar = probe.BaseStarCost;
			var beforeVars = SnapshotNumericDynamicVars(probe);

			probe.UpgradeInternal();
			probe.FinalizeUpgradeInternal();

			if (!energyCostsX)
			{
				var afterEnergy = probe.EnergyCost.GetWithModifiers(CostModifiers.None);
				if (afterEnergy < beforeEnergy)
					return true;
			}

			if (!hasStarCostX && beforeStar >= 0)
			{
				var afterStar = probe.BaseStarCost;
				if (afterStar >= 0 && afterStar < beforeStar)
					return true;
			}

			var afterVars = SnapshotNumericDynamicVars(probe);
			foreach (var key in beforeVars.Keys.Union(afterVars.Keys))
			{
				var b = beforeVars.GetValueOrDefault(key);
				var a = afterVars.GetValueOrDefault(key);
				if (DecimalsEqual(b, a))
					continue;
				if (DynamicVarChangeIsImprovement(key, b, a))
					return true;
			}

			return false;
		}
		catch
		{
			return true;
		}
	}

	private static Dictionary<string, decimal> SnapshotNumericDynamicVars(CardModel c)
	{
		var d = new Dictionary<string, decimal>(StringComparer.Ordinal);
		foreach (var kv in c.DynamicVars)
		{
			if (kv.Value is StringVar)
				continue;
			d[kv.Key] = kv.Value.BaseValue;
		}

		return d;
	}

	private static bool DecimalsEqual(decimal x, decimal y) => Math.Abs(x - y) < 0.0001m;

	private static bool DynamicVarChangeIsImprovement(string key, decimal before, decimal after)
	{
		if (key.Equals("HpLoss", StringComparison.Ordinal))
			return after < before;
		return after > before;
	}	/// <summary>锐锋、重刃、超巨化等：需牌面带正数的「打出」伤害动态变量（含 Move 伤害、ExtraDamage、CalculatedDamage 等）。</summary>
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

	/// <summary>华丽等：打出时移动伤害或正数自伤（<see cref="HpLossVar"/>）。</summary>
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
