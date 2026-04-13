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
/// <see cref="StarlightStarsEnchantment"/>：与原版 affliction overlay 相同管线——
/// <c>scenes/cards/overlays/afflictions/lunar_starlight.tscn</c>（Control + <see cref="Sprite2D"/>，见 smog/hexed）。
/// </summary>
[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
internal static class NCardStarlightOverlayPatch
{
	/// <summary>改为 <c>true</c> 可重新启用星光 lunar 牌面叠层。</summary>
	private static readonly bool EnableStarlightCardOverlay = false;

	/// <summary>与 <see cref="SceneHelper.GetScenePath"/> 约定一致：<c>res://scenes/</c> + inner + <c>.tscn</c>。</summary>
	private const string LunarStarlightOverlayInnerPath = "cards/overlays/afflictions/lunar_starlight";

	private static readonly System.Reflection.FieldInfo OverlayContainerField =
		AccessTools.Field(typeof(NCard), "_overlayContainer")!;

	private static readonly System.Reflection.FieldInfo CardOverlayField =
		AccessTools.Field(typeof(NCard), "_cardOverlay")!;

	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		CardModel? model = __instance.Model;
		if (model?.Enchantment is not StarlightStarsEnchantment)
			return;
		if (!EnableStarlightCardOverlay)
			return;

		var overlayContainer = (Node)OverlayContainerField.GetValue(__instance)!;
		var oldOverlay = (Control?)CardOverlayField.GetValue(__instance);
		if (oldOverlay != null)
		{
			overlayContainer.RemoveChildSafely(oldOverlay);
			oldOverlay.QueueFreeSafely();
			CardOverlayField.SetValue(__instance, null);
		}

		string path = SceneHelper.GetScenePath(LunarStarlightOverlayInnerPath);
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
