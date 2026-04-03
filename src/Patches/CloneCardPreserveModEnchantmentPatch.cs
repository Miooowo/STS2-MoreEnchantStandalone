using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.Patches;

/// <summary>
///     部分环境下 <see cref="CardModel" /> 克隆进战斗/其它流程后附魔丢失，伤害与预览不计算 MOD 附魔。
///     若源卡有附魔而克隆没有，则从源附魔再应用一次。
/// </summary>
internal static class CloneCardPreserveModEnchantmentPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(CombatState), nameof(CombatState.CloneCard))]
	private static void CombatStateClonePostfix(CardModel mutableCard, ref CardModel __result)
	{
		RestoreEnchantmentIfLost(mutableCard, ref __result);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(RunState), nameof(RunState.CloneCard))]
	private static void RunStateClonePostfix(CardModel mutableCard, ref CardModel __result)
	{
		RestoreEnchantmentIfLost(mutableCard, ref __result);
	}

	private static void RestoreEnchantmentIfLost(CardModel source, ref CardModel clone)
	{
		if (source.Enchantment == null || clone.Enchantment != null)
			return;

		var enchCopy = (EnchantmentModel)source.Enchantment.ClonePreservingMutability();
		clone.EnchantInternal(enchCopy, enchCopy.Amount);
		clone.Enchantment!.ModifyCard();
		clone.FinalizeUpgradeInternal();
	}
}
