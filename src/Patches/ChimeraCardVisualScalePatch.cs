using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 生活质量：精小/笨重附魔改变卡牌视觉尺寸（仅视觉，不影响碰撞与数值）。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateEnchantmentVisuals")]
internal static class ChimeraCardVisualScalePatch
{
	private const float CompactScale = 0.8f;
	private const float BulkyScale = 1.2f;

	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		if (__instance.Body == null)
			return;

		float scale = ResolveScale(__instance);
		__instance.Body.Scale = Vector2.One * scale;
	}

	private static float ResolveScale(NCard card)
	{
		var enchantment = card.Model?.Enchantment;
		return enchantment switch
		{
			ChimeraCompactEnchantment => CompactScale,
			ChimeraBulkyEnchantment => BulkyScale,
			_ => 1f
		};
	}
}
