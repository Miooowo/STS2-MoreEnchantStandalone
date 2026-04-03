using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// <see cref="CardModel.DeepCloneFields"/> 会 <see cref="CardModel.EnchantInternal"/> 但不会 <see cref="EnchantmentModel.ModifyCard"/>，
/// 打击附魔依赖的 <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag.Strike"/> 会丢失。
/// </summary>
[HarmonyPatch(typeof(CardModel), "DeepCloneFields")]
internal static class DeepCloneReapplyStrikeEnchantPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance)
	{
		if (__instance.Enchantment is ChimeraStrikeEnchantment)
			CardStrikeTagUtil.ApplyStrikeTag(__instance);
	}
}
