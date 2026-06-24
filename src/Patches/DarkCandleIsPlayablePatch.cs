using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Patches;

/// <summary>幽暗烛火：允许打出原本不可打出的诅咒牌（放开 IsPlayable）。</summary>
[HarmonyPatch(typeof(CardModel), MethodType.Getter)]
[HarmonyPatch("IsPlayable")]
internal static class DarkCandleIsPlayablePatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (__result)
			return;
		if (!DarkCandleIsPlayablePatch_Helpers.HasDarkCandle(__instance))
			return;
		if (__instance.Owner == null)
			return;

		__result = true;
	}
}

/// <summary>幽暗烛火：放开 CardModel.CanPlay() 判定，覆盖实际出牌路径。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), new Type[] { })]
internal static class DarkCandleCanPlayPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref bool __result)
	{
		if (__result)
			return;
		if (__instance.Owner == null)
			return;
		if (!DarkCandleIsPlayablePatch_Helpers.HasDarkCandle(__instance))
			return;

		__result = true;
	}
}

/// <summary>幽暗烛火：放开带 reason 的 CanPlay 判定，并清理阻止原因。</summary>
[HarmonyPatch]
internal static class DarkCandleCanPlayWithReasonPatch
{
	private static Type? _reasonType;

	private static MethodBase? TargetMethod()
	{
		var method = typeof(CardModel)
			.GetMethods(BindingFlags.Instance | BindingFlags.Public)
			.FirstOrDefault(m =>
			{
				if (m.Name != nameof(CardModel.CanPlay))
					return false;
				var ps = m.GetParameters();
				return ps.Length == 2 &&
				       ps[0].ParameterType.IsByRef &&
				       ps[1].ParameterType.IsByRef &&
				       ps[1].ParameterType.GetElementType() == typeof(AbstractModel);
			});

		_reasonType = method?.GetParameters()[0].ParameterType.GetElementType();
		return method;
	}

	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref bool __result, object[] __args)
	{
		if (__result)
			return;
		if (__instance.Owner == null)
			return;
		if (!DarkCandleIsPlayablePatch_Helpers.HasDarkCandle(__instance))
			return;

		__result = true;
		if (__args.Length >= 2)
		{
			if (_reasonType != null)
				__args[0] = Activator.CreateInstance(_reasonType) ?? __args[0];
			__args[1] = null!;
		}
	}
}

internal static class DarkCandleIsPlayablePatch_Helpers
{
	internal static bool HasDarkCandle(CardModel card)
	{
		if (card.Enchantment is DarkCandleEnchantment)
			return true;

		if (!MultiEnchantmentCompat.IsAvailable())
			return false;

		var supportType = System.Type.GetType("MultiEnchantmentMod.MultiEnchantmentSupport, MultiEnchantmentMod", throwOnError: false);
		var getEnchantments = supportType?.GetMethod("GetEnchantments", new[] { typeof(CardModel) });
		if (getEnchantments == null)
			return false;

		try
		{
			if (getEnchantments.Invoke(null, new object[] { card }) is not System.Collections.IEnumerable all)
				return false;
			foreach (var enchantment in all)
			{
				if (enchantment is DarkCandleEnchantment)
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
