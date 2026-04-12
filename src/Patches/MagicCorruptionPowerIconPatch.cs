using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MoreEnchant.Powers;

namespace MoreEnchant.Patches;

/// <summary>魔法腐化能力使用原版 <see cref="CorruptionPower"/> 的图集与立绘路径。</summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.PackedIconPath), MethodType.Getter)]
internal static class MagicCorruptionPowerIconPatch
{
	private static string? _corruptionPackedPath;

	[HarmonyPostfix]
	private static void Postfix(PowerModel __instance, ref string __result)
	{
		if (__instance is not MagicCorruptionPower)
			return;

		_corruptionPackedPath ??= ModelDb.GetById<PowerModel>(ModelDb.GetId(typeof(CorruptionPower))).PackedIconPath;
		__result = _corruptionPackedPath;
	}
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ResolvedBigIconPath), MethodType.Getter)]
internal static class MagicCorruptionPowerBigIconPatch
{
	private static string? _corruptionBigPath;

	[HarmonyPostfix]
	private static void Postfix(PowerModel __instance, ref string __result)
	{
		if (__instance is not MagicCorruptionPower)
			return;

		_corruptionBigPath ??= ModelDb.GetById<PowerModel>(ModelDb.GetId(typeof(CorruptionPower))).ResolvedBigIconPath;
		__result = _corruptionBigPath;
	}
}
