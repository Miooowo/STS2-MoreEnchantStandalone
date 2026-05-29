using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MoreEnchant.Powers;

namespace MoreEnchant.Patches;

/// <summary>
/// 为灵魂实体应用低透明视觉表现。
/// </summary>
[HarmonyPatch]
internal static class SoulDetachmentVisualPatch
{
	private const float SoulAlpha = 0.42f;

	[HarmonyPatch(typeof(NCreature), "_Ready")]
	[HarmonyPostfix]
	private static void AfterReady(NCreature __instance)
	{
		ApplySoulAlpha(__instance);
	}

	[HarmonyPatch(typeof(NCreature), "OnPowerApplied")]
	[HarmonyPostfix]
	private static void AfterPowerApplied(NCreature __instance)
	{
		ApplySoulAlpha(__instance);
	}

	[HarmonyPatch(typeof(NCreature), "OnPowerRemoved")]
	[HarmonyPostfix]
	private static void AfterPowerRemoved(NCreature __instance)
	{
		ApplySoulAlpha(__instance);
	}

	private static void ApplySoulAlpha(NCreature creatureNode)
	{
		if (creatureNode.Visuals == null || creatureNode.Entity == null)
			return;

		float alpha = creatureNode.Entity.GetPower<SoulDetachmentLinkPower>() != null ? SoulAlpha : 1f;
		creatureNode.Visuals.Modulate = new Color(1f, 1f, 1f, alpha);
	}
}
