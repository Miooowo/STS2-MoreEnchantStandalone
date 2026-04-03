using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone;

[HarmonyPatch(typeof(ModelDb), "get_DebugEnchantments")]
internal static class ModelDbDebugEnchantmentsAppendPatch
{
	[HarmonyPostfix]
	private static void Postfix(ref IEnumerable<EnchantmentModel> __result)
	{
		__result = __result.Concat(MoreEnchantEnchantmentRegistry.ResolveAppended()).Distinct();
	}
}
