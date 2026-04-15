using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MoreEnchant;
using MoreEnchant.Enchantments;

namespace MoreEnchant.EnchantmentCompendium;

/// <summary>布局与 <c>screens/potion_lab/potion_lab.tscn</c> 一致：可滚动内容、原版滚动条、边缘渐变、按奖励品质分多段药水分类样式。</summary>
internal sealed partial class EnchantmentBrowserRoot : Control
{
	private const string PotionCategoryScenePath = "res://scenes/screens/potion_lab/potion_category.tscn";
	private const string ScrollbarScenePath = "res://scenes/ui/scrollbar.tscn";
	private const string BackButtonScenePath = "res://scenes/ui/back_button.tscn";

	private static readonly EnchantmentRewardRarity[] RarityColumnOrder =
	[
		EnchantmentRewardRarity.Common,
		EnchantmentRewardRarity.Uncommon,
		EnchantmentRewardRarity.Curse,
		EnchantmentRewardRarity.Rare,
		EnchantmentRewardRarity.Special,
	];

	private readonly IReadOnlyList<EnchantmentModel> _models;
	private VBoxContainer? _categoriesRoot;
	private NBackButton? _backButton;

	public EnchantmentBrowserRoot(IReadOnlyList<EnchantmentModel> models)
	{
		_models = models;
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;

		var dim = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 0.92f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		dim.GrowHorizontal = GrowDirection.Both;
		dim.GrowVertical = GrowDirection.Both;
		AddChild(dim);

		var screenContents = new NScrollableContainer { Name = "ScreenContents" };
		screenContents.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		screenContents.GrowHorizontal = GrowDirection.Both;
		screenContents.GrowVertical = GrowDirection.Both;
		((CanvasItem)screenContents).Modulate = new Color(1f, 1f, 1f, 0f);

		var content = new MarginContainer { Name = "Content", MouseFilter = MouseFilterEnum.Ignore };
		content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		content.GrowHorizontal = GrowDirection.Both;
		content.GrowVertical = GrowDirection.Both;
		content.AddThemeConstantOverride("margin_top", 150);
		content.AddThemeConstantOverride("margin_bottom", 150);

		_categoriesRoot = new VBoxContainer
		{
			Name = "CategoriesRoot",
			CustomMinimumSize = new Vector2(1020, 0),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_categoriesRoot.AddThemeConstantOverride("separation", 18);

		content.AddChild(_categoriesRoot);
		screenContents.AddChild(content);

		var scrollbarScene = ResourceLoader.Load<PackedScene>(ScrollbarScenePath);
		if (scrollbarScene != null)
		{
			var scrollbar = scrollbarScene.Instantiate<NScrollbar>();
			scrollbar.LayoutMode = 1;
			scrollbar.AnchorLeft = 1f;
			scrollbar.AnchorRight = 1f;
			scrollbar.AnchorBottom = 1f;
			scrollbar.OffsetLeft = -100f;
			scrollbar.OffsetTop = 130f;
			scrollbar.OffsetRight = -50f;
			scrollbar.OffsetBottom = -130f;
			scrollbar.GrowHorizontal = Control.GrowDirection.Begin;
			scrollbar.GrowVertical = Control.GrowDirection.Both;
			screenContents.AddChild(scrollbar);
		}

		var border = CreateBorderGradient();
		screenContents.AddChild(border);

		AddChild(screenContents);

		var backScene = ResourceLoader.Load<PackedScene>(BackButtonScenePath);
		if (backScene != null)
		{
			_backButton = backScene.Instantiate<NBackButton>();
			((CanvasItem)_backButton).ZIndex = 8;
			((GodotObject)_backButton).Connect(
				NClickableControl.SignalName.Released,
				Callable.From<NButton>(_ => QueueFree()),
				0u);
			AddChild(_backButton);
		}

		CallDeferred(nameof(DeferredAfterTree));
	}

	private static TextureRect CreateBorderGradient()
	{
		// 默认仅 2 个色标，须 AddPoint 后再有 4 档；勿对不存在的索引 SetColor/SetOffset。
		var gradient = new Gradient { InterpolationMode = Gradient.InterpolationModeEnum.Cubic };
		gradient.SetColor(0, new Color(0f, 0f, 0f, 0.9f));
		gradient.SetOffset(0, 0f);
		gradient.SetColor(1, new Color(0f, 0f, 0f, 0.9f));
		gradient.SetOffset(1, 1f);
		gradient.AddPoint(0.05f, new Color(0f, 0f, 0f, 0f));
		gradient.AddPoint(0.95f, new Color(0f, 0f, 0f, 0f));
		for (var i = 0; i < gradient.GetPointCount(); i++)
		{
			var o = gradient.GetOffset(i);
			if (o <= 0.001f || o >= 0.999f)
				gradient.SetColor(i, new Color(0f, 0f, 0f, 0.9f));
			else
				gradient.SetColor(i, new Color(0f, 0f, 0f, 0f));
		}

		var tex = new GradientTexture2D
		{
			Gradient = gradient,
			Width = 2,
			Height = 256,
			FillTo = new Vector2(0, 1),
		};

		var border = new TextureRect
		{
			Name = "BorderGradient",
			MouseFilter = MouseFilterEnum.Ignore,
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
		};
		border.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		border.GrowHorizontal = GrowDirection.Both;
		border.GrowVertical = GrowDirection.Both;
		border.Scale = new Vector2(1f, 1.01f);
		border.PivotOffset = new Vector2(0f, 540f);
		return border;
	}

	private void DeferredAfterTree()
	{
		_backButton?.Enable();

		var screenContents = GetNodeOrNull<NScrollableContainer>("ScreenContents");
		if (screenContents != null)
		{
			CreateTween()?.TweenProperty(
				(GodotObject)(object)screenContents,
				new NodePath("modulate:a"),
				1f,
				0.25f);
		}

		if (_categoriesRoot == null)
			return;

		var catScene = ResourceLoader.Load<PackedScene>(PotionCategoryScenePath);
		if (catScene == null)
		{
			QueueFree();
			return;
		}

		var grouped = _models
			.GroupBy(e => EnchantmentRewardRarityUtil.GetForTemplate(e))
			.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Title.GetFormattedText()).ToList());

		foreach (var rarity in RarityColumnOrder)
		{
			if (!grouped.TryGetValue(rarity, out var list) || list.Count == 0)
				continue;

			var category = catScene.Instantiate<NPotionLabCategory>();
			var header = category.GetNode<MegaRichTextLabel>("Header");
			header.Text = HeaderForRarity(rarity);

			var grid = category.GetNode<GridContainer>("%PotionsContainer");
			foreach (var model in list)
				grid.AddChild(EnchantmentCompendiumEntry.Create(model));

			_categoriesRoot.AddChild(category);
		}

		CallDeferred(nameof(DeferredFocusFirst));
		screenContents?.CallDeferred("DisableScrollingIfContentFits");
	}

	private static string HeaderForRarity(EnchantmentRewardRarity rarity)
	{
		var key = rarity switch
		{
			EnchantmentRewardRarity.Common => "COMPENDIUM_ENCHANT_BROWSER.header.common",
			EnchantmentRewardRarity.Uncommon => "COMPENDIUM_ENCHANT_BROWSER.header.uncommon",
			EnchantmentRewardRarity.Curse => "COMPENDIUM_ENCHANT_BROWSER.header.curse",
			EnchantmentRewardRarity.Rare => "COMPENDIUM_ENCHANT_BROWSER.header.rare",
			EnchantmentRewardRarity.Special => "COMPENDIUM_ENCHANT_BROWSER.header.special",
			_ => "",
		};
		if (string.IsNullOrEmpty(key))
			return "";
		return new LocString("main_menu_ui", key).GetFormattedText();
	}

	private void DeferredFocusFirst()
	{
		if (_categoriesRoot == null)
			return;

		foreach (var child in _categoriesRoot.GetChildren())
		{
			if (child is not NPotionLabCategory cat)
				continue;
			var grid = cat.GetNodeOrNull<GridContainer>("%PotionsContainer");
			if (grid == null || grid.GetChildCount() == 0)
				continue;
			if (grid.GetChild(0) is EnchantmentCompendiumEntry first)
			{
				first.SuppressHoverTipForNextFocus();
				first.GrabFocus();
				return;
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetViewport()?.SetInputAsHandled();
			QueueFree();
			return;
		}

		base._UnhandledInput(@event);
	}
}
