using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace MoreEnchant.EnchantmentCompendium;

/// <summary>在图鉴（Compendium）顶部行增加「附魔图鉴」入口，打开与药水研究所/遗物收集同风格的浏览界面。</summary>
[HarmonyPatch(typeof(NCompendiumSubmenu), "_Ready")]
internal static class CompendiumEnchantmentBrowserPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCompendiumSubmenu __instance)
	{
		var topRow = __instance.GetNodeOrNull<HBoxContainer>("MarginContainer/VBoxContainer/TopRow");
		if (topRow == null || topRow.GetNodeOrNull(EnchantmentCompendiumConstants.CompendiumButtonName) != null)
			return;

		var scene = ResourceLoader.Load<PackedScene>(EnchantmentCompendiumConstants.SubmenuButtonScenePath);
		if (scene == null)
			return;

		var btn = scene.Instantiate<NShortSubmenuButton>();
		btn.Name = EnchantmentCompendiumConstants.CompendiumButtonName;
		btn.FocusMode = Control.FocusModeEnum.All;
		topRow.AddChild(btn);

		var title = btn.GetNode<MegaLabel>("%Title");
		title.SetTextAutoSize(
			new LocString("main_menu_ui", "COMPENDIUM_ENCHANT_BROWSER.button.title").GetFormattedText());
		var desc = btn.GetNode<MegaRichTextLabel>("%Description");
		desc.Text = new LocString("main_menu_ui", "COMPENDIUM_ENCHANT_BROWSER.button.description")
			.GetFormattedText();
		var icon = btn.GetNode<TextureRect>("Icon");
		var iconPath = ImageHelper.GetImagePath(EnchantmentCompendiumConstants.CompendiumCoverImagePath);
		if (iconPath != null && ResourceLoader.Exists(iconPath, string.Empty))
			icon.Texture = (Texture2D)(object)PreloadManager.Cache.GetCompressedTexture2D(iconPath);

		((GodotObject)btn).Connect(
			NClickableControl.SignalName.Released,
			Callable.From<NButton>(_ => EnchantmentBrowserOverlay.Show(__instance)),
			0u);

		RewireCompendiumFocus(__instance, topRow);
	}

	private static void RewireCompendiumFocus(NCompendiumSubmenu compendium, HBoxContainer topRow)
	{
		var topControls = topRow.GetChildren().OfType<Control>().ToList();
		var bottomControls = new List<Control>
		{
			compendium.GetNode<Control>("%LeaderboardsButton"),
			compendium.GetNode<Control>("%StatisticsButton"),
			compendium.GetNode<Control>("%RunHistoryButton"),
		};

		for (var i = 0; i < topControls.Count; i++)
		{
			topControls[i].FocusNeighborTop = topControls[i].GetPath();
			topControls[i].FocusNeighborLeft = (i > 0 ? topControls[i - 1] : topControls[i]).GetPath();
			topControls[i].FocusNeighborRight = (i < topControls.Count - 1 ? topControls[i + 1] : topControls[i]).GetPath();
		}

		for (var j = 0; j < bottomControls.Count; j++)
		{
			bottomControls[j].FocusNeighborBottom = bottomControls[j].GetPath();
			bottomControls[j].FocusNeighborLeft = (j > 0 ? bottomControls[j - 1] : bottomControls[j]).GetPath();
			bottomControls[j].FocusNeighborRight = (j < bottomControls.Count - 1 ? bottomControls[j + 1] : bottomControls[j]).GetPath();
		}

		topControls[0].FocusNeighborBottom = bottomControls[0].GetPath();
		topControls[1].FocusNeighborBottom = bottomControls[0].GetPath();
		topControls[2].FocusNeighborBottom = bottomControls[1].GetPath();
		topControls[3].FocusNeighborBottom = bottomControls[1].GetPath();
		if (topControls.Count >= 5)
			topControls[4].FocusNeighborBottom = bottomControls[2].GetPath();

		bottomControls[0].FocusNeighborTop = topControls[1].GetPath();
		bottomControls[1].FocusNeighborTop = topControls[2].GetPath();
		bottomControls[2].FocusNeighborTop = (topControls.Count >= 5 ? topControls[4] : topControls[3]).GetPath();
	}
}
