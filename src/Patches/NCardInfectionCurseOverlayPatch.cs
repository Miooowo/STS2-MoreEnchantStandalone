using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// <see cref="InfectionCurseEnchantment"/>：使用与状态牌 <see cref="MegaCrit.Sts2.Core.Models.Cards.Infection"/> 相同的 overlay 场景（<c>cards/overlays/infection</c>）。
/// </summary>
[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
internal static class NCardInfectionCurseOverlayPatch
{
	private static readonly string InfectionOverlayInnerPath = "cards/overlays/infection";

	private static readonly System.Reflection.FieldInfo OverlayContainerField =
		AccessTools.Field(typeof(NCard), "_overlayContainer")!;

	private static readonly System.Reflection.FieldInfo CardOverlayField =
		AccessTools.Field(typeof(NCard), "_cardOverlay")!;

	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		CardModel? model = __instance.Model;
		if (model?.Enchantment is not InfectionCurseEnchantment)
			return;

		var overlayContainer = (Node)OverlayContainerField.GetValue(__instance)!;
		var oldOverlay = (Control?)CardOverlayField.GetValue(__instance);
		if (oldOverlay != null)
		{
			overlayContainer.RemoveChildSafely(oldOverlay);
			oldOverlay.QueueFreeSafely();
			CardOverlayField.SetValue(__instance, null);
		}

		string path = SceneHelper.GetScenePath(InfectionOverlayInnerPath);
		if (!ResourceLoader.Exists(path, string.Empty))
			return;

		Node root = PreloadManager.Cache.GetScene(path).Instantiate();
		if (root is not Control created)
		{
			root.QueueFree();
			return;
		}

		overlayContainer.AddChildSafely(created);
		CardOverlayField.SetValue(__instance, created);
	}
}
