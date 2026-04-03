using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone;

[HarmonyPatch(typeof(EnchantmentModel), "get_IntendedIconPath")]
internal static class EnchantmentIntendedIconPathPatch
{
	[HarmonyPrefix]
	private static bool Prefix(EnchantmentModel __instance, ref string __result)
	{
		if (__instance is not IModEnchantmentAssetOverrides ov)
			return true;
		var path = ov.CustomIconPath;
		if (string.IsNullOrWhiteSpace(path))
			return true;
		if (!ResourceLoader.Exists(path))
			return true;
		__result = path;
		return false;
	}
}
