using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone;

/// <summary>
/// 本模组 <see cref="ModEnchantmentTemplate"/> 若未在资源中提供 <c>enchantments/&lt;id&gt;.png</c>，
/// 在语言为简体中文（<c>zhs</c>）时，将 <see cref="EnchantmentModel.IconPath"/> 从原版 missing 贴图改为模组内附魔书图标（源资源 <c>images/enchantments/enchanted_book.gif</c>，Godot 无法导入 GIF，故使用同目录导出的 <c>enchanted_book.png</c> 纹理；需在 PCK 中存在）；其他语言仍用原版 missing。
/// </summary>
[HarmonyPatch(typeof(EnchantmentModel), "get_IconPath")]
	internal static class EnchantmentModMissingIconFallbackPatch
{
	private const string SimplifiedChineseLanguageCode = "zhs";

	private static readonly FieldInfo IconPathCacheField =
		AccessTools.Field(typeof(EnchantmentModel), "_iconPath")!;

	private static readonly string FallbackPath =
		ImageHelper.GetImagePath("enchantments/enchanted_book.png");

	[HarmonyPostfix]
	private static void Postfix(EnchantmentModel __instance, ref string __result)
	{
		if (__result != EnchantmentModel.MissingIconPath)
			return;
		if (LocManager.Instance == null || LocManager.Instance.Language != SimplifiedChineseLanguageCode)
			return;
		if (!typeof(ModEnchantmentTemplate).IsAssignableFrom(__instance.GetType()))
			return;
		if (!ResourceLoader.Exists(FallbackPath, string.Empty))
			return;
		__result = FallbackPath;
		IconPathCacheField.SetValue(__instance, FallbackPath);
	}
}
