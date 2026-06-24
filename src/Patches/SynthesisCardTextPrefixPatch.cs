using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 合成附魔：将「合[gold]合成[/gold]」固定插入卡牌描述首行，提升识别度。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), new[] { typeof(PileType), typeof(Creature) })]
internal static class SynthesisCardTextPrefixPatch
{
	private const string CraftLeadTextLocTable = "enchantments";
	private const string CraftLeadTextLocKey = "SYNTHESIS_ENCHANTMENT.craftLeadText";
	private const string CraftLeadTextFallback = "[gold]合成[/gold]。";

	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (string.IsNullOrWhiteSpace(__result))
			return;
		if (!HasSynthesisEnchantment(__instance))
			return;

		var craftLeadText = ResolveCraftLeadText();
		var firstLine = __result.Split('\n').FirstOrDefault()?.Trim();
		if (string.Equals(firstLine, craftLeadText, StringComparison.Ordinal))
			return;

		__result = $"{craftLeadText}\n{__result}";
	}

	private static string ResolveCraftLeadText()
	{
		var loc = new LocString(CraftLeadTextLocTable, CraftLeadTextLocKey);
		if (!loc.Exists())
			return CraftLeadTextFallback;

		var text = loc.GetFormattedText();
		return string.IsNullOrWhiteSpace(text) ? CraftLeadTextFallback : text;
	}

	private static bool HasSynthesisEnchantment(CardModel card)
	{
		if (card.Enchantment is SynthesisEnchantment)
			return true;

		// 兼容多附魔：检测额外附魔列表中是否存在「合成」。
		var memSupportType = Type.GetType("MultiEnchantmentMod.MultiEnchantmentSupport, MultiEnchantmentMod", throwOnError: false);
		var getEnchantments = memSupportType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == "GetEnchantments" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(card));
		if (getEnchantments == null)
			return false;

		try
		{
			if (getEnchantments.Invoke(null, new object[] { card }) is not IEnumerable all)
				return false;
			foreach (var enchantment in all)
			{
				if (enchantment is SynthesisEnchantment)
					return true;
			}
		}
		catch
		{
			// optional compat, ignore failures
		}

		return false;
	}
}
