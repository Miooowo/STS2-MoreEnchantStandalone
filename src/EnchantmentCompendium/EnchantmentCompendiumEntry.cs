using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace MoreEnchant.EnchantmentCompendium;

/// <summary>行为对齐 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection.NRelicCollectionEntry"/>：缩放与原版悬浮说明。</summary>
internal sealed partial class EnchantmentCompendiumEntry : NButton
{
	private EnchantmentModel _model = null!;
	private TextureRect _iconRect = null!;
	private Tween? _hoverTween;

	/// <summary>图鉴打开时对首格 <see cref="Control.GrabFocus"/> 会触发 <see cref="OnFocus"/>，但不希望未操作就出现悬浮说明。</summary>
	private bool _suppressHoverTipForNextFocus;

	public static EnchantmentCompendiumEntry Create(EnchantmentModel model)
	{
		var e = new EnchantmentCompendiumEntry
		{
			_model = model,
			CustomMinimumSize = new Vector2(68, 68),
			FocusMode = FocusModeEnum.All,
			MouseDefaultCursorShape = CursorShape.PointingHand,
		};
		return e;
	}

	/// <summary>下一次获得焦点时跳过缩放与悬浮说明（用于仅建立键盘/手柄焦点起点）。</summary>
	internal void SuppressHoverTipForNextFocus() => _suppressHoverTipForNextFocus = true;

	public override void _Ready()
	{
		ConnectSignals();

		var holder = new Control
		{
			Name = "IconHolder",
			CustomMinimumSize = new Vector2(68, 68),
			MouseFilter = MouseFilterEnum.Ignore,
			PivotOffset = new Vector2(34, 34),
		};
		holder.LayoutMode = 1;
		holder.SetAnchorsPreset(LayoutPreset.Center);
		holder.OffsetLeft = -34f;
		holder.OffsetTop = -34f;
		holder.OffsetRight = 34f;
		holder.OffsetBottom = 34f;
		AddChild(holder);

		_iconRect = new TextureRect
		{
			LayoutMode = 1,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		};
		_iconRect.SetAnchorsPreset(LayoutPreset.FullRect);
		_iconRect.PivotOffset = new Vector2(34f, 34f);
		_iconRect.Texture = (Texture2D)(object)_model.Icon;
		holder.AddChild(_iconRect);
	}

	protected override void OnFocus()
	{
		if (_suppressHoverTipForNextFocus)
		{
			_suppressHoverTipForNextFocus = false;
			return;
		}

		_hoverTween?.Kill();
		_hoverTween = CreateTween();
		_hoverTween.TweenProperty(
			(GodotObject)(object)_iconRect,
			new NodePath("scale"),
			Variant.From(Vector2.One * 1.25f),
			0.5f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

		var tips = _model.HoverTips;
		var set = NHoverTipSet.CreateAndShow(this, tips, HoverTip.GetHoverTipAlignment(this));
		set.SetFollowOwner();
	}

	protected override void OnUnfocus()
	{
		_hoverTween?.Kill();
		_hoverTween = CreateTween();
		_hoverTween.TweenProperty(
			(GodotObject)(object)_iconRect,
			new NodePath("scale"),
			Variant.From(Vector2.One),
			0.5f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		NHoverTipSet.Remove(this);
	}

	protected override void OnRelease()
	{
		_hoverTween?.Kill();
		_hoverTween = CreateTween();
		_hoverTween.TweenProperty(
			(GodotObject)(object)_iconRect,
			new NodePath("scale"),
			Variant.From(Vector2.One),
			0.5f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		NHoverTipSet.Remove(this);
	}
}
