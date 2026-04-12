using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>多打：在力量攻击的 <see cref="AttackCommand"/> 上增加命中段数。</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyAttackHitCount))]
internal static class ExtraHitEnchantmentAttackPatch
{
	[HarmonyPostfix]
	private static void Postfix(AttackCommand attackCommand, ref decimal __result)
	{
		if (attackCommand.ModelSource is not CardModel card || card.Enchantment is not ExtraHitEnchantment extra)
			return;
		if (!ValuePropCombatUtil.IsPoweredAttackMove(attackCommand.DamageProps))
			return;

		__result += extra.Amount;
	}
}
