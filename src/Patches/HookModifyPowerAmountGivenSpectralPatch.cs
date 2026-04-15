using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 幽灵：在施加/叠层能力时一次性把「与格挡相关的能力数值」×1.5（卡面 PowerVar 与战斗内一致），
/// 不再在 <see cref="Hook.ModifyBlock"/> 对 Unpowered 格挡二次乘算，避免「打出两次」式的双重收益。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountGiven))]
internal static class HookModifyPowerAmountGivenSpectralPatch
{
	private const decimal BlockMultiplier = 1.5m;

	[HarmonyPostfix]
	private static void Postfix(ref decimal __result, CombatState combatState, PowerModel power, Creature giver, decimal amount,
		Creature? target, CardModel? cardSource)
	{
		_ = combatState;
		_ = giver;
		_ = amount;
		_ = target;
		if (cardSource?.Enchantment is not SpectralEtherealEnchantment)
			return;
		if (!SpectralPowerAmountEligible.AllowsScaling(power))
			return;

		__result = decimal.Round(__result * BlockMultiplier, MidpointRounding.AwayFromZero);
	}
}
