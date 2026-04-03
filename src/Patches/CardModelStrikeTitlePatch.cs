using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>奇美拉打击附魔：卡名末尾追加附魔标题。中文直接拼接；英文等语言在卡名与后缀之间保留空格。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
internal static class CardModelStrikeTitlePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (__instance.Enchantment is not ChimeraStrikeEnchantment ench)
			return;

		var suffix = ench.Title;
		if (!suffix.Exists())
			return;

		var joiner = IsChineseDisplayLocale() ? "" : " ";
		__result += joiner + suffix.GetFormattedText();
	}

	/// <summary>与游戏内简中/繁中界面一致：不在汉字标题与后缀之间插 ASCII 空格。</summary>
	private static bool IsChineseDisplayLocale()
	{
		string? raw = null;
		try
		{
			raw = LocManager.Instance?.Language;
		}
		catch
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(raw))
			return false;

		var t = raw.Trim().Replace('-', '_').ToLowerInvariant();
		return t is "zhs" or "zh_cn" or "zh_hans" or "zh_sg" or "zht" or "zh_tw" or "zh_hk" or "zh_mo"
			|| t.StartsWith("zh_", StringComparison.Ordinal)
			|| t == "zh";
	}
}
