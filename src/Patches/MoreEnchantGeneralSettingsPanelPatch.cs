using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MoreEnchant.Patches;

/// <summary>在设置 → 常规页底部注入 More Enchant 模组选项，并写回 <see cref="MoreEnchantSettingsStore"/>。</summary>
[HarmonyPatch(typeof(NSettingsPanel), nameof(NSettingsPanel._Ready))]
internal static class MoreEnchantGeneralSettingsPanelPatch
{
	private const string InjectedRootName = "MoreEnchantSettingsRoot";

	private static readonly MethodInfo? RefreshSizeMethod =
		typeof(NSettingsPanel).GetMethod("RefreshSize", BindingFlags.Instance | BindingFlags.NonPublic);

	[HarmonyPostfix]
	private static void Postfix(NSettingsPanel __instance)
	{
		if (__instance.Name != "GeneralSettings")
			return;

		var content = __instance.Content;
		if (content == null)
			return;
		if (content.GetNodeOrNull(InjectedRootName) != null)
			return;

		var settings = MoreEnchantSettingsStore.Get();

		var root = new VBoxContainer
		{
			Name = InjectedRootName,
			MouseFilter = Control.MouseFilterEnum.Pass,
		};
		root.AddThemeConstantOverride("separation", 8);

		root.AddChild(CreateDivider());
		root.AddChild(CreateSectionTitle("More Enchant（更多附魔）"));

		var chanceRow = CreateLabeledRow(
			"卡牌奖励附魔概率（%）",
			out var chanceSlider,
			out var chanceValueLabel);
		chanceSlider.MinValue = 0;
		chanceSlider.MaxValue = 100;
		chanceSlider.Step = 1;
		chanceSlider.FocusMode = Control.FocusModeEnum.None;
		chanceSlider.Value = settings.RewardEnchantChancePercent;
		chanceValueLabel.Text = $"{settings.RewardEnchantChancePercent}%";
		chanceSlider.ValueChanged += v =>
		{
			settings.RewardEnchantChancePercent = Mathf.Clamp((int)v, 0, 100);
			chanceValueLabel.Text = $"{settings.RewardEnchantChancePercent}%";
			MoreEnchantSettingsStore.PersistCurrent();
		};
		root.AddChild(chanceRow);

		var mpNote = new Label
		{
			Text = "联机时以下选项以房主（Host）设置为准（含附魔 Beta）。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		root.AddChild(mpNote);

		var betaEnchantRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var betaEnchantCheck = new CheckBox
		{
			Text = "附魔 Beta：夹击、恶魔护盾（随机奖励池）",
			ButtonPressed = settings.BetaRewardEnchantmentsEnabled,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		betaEnchantRow.AddChild(betaEnchantCheck);
		root.AddChild(betaEnchantRow);

		var shopEnableRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var shopCheck = new CheckBox
		{
			Text = "商店卡牌随机附魔",
			ButtonPressed = settings.ShopEnchantEnabled,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		shopEnableRow.AddChild(shopCheck);
		root.AddChild(shopEnableRow);

		var shopChanceRow = CreateLabeledRow(
			"商店附魔概率（%）",
			out var shopChanceSlider,
			out var shopChanceLabel);
		shopChanceSlider.MinValue = 0;
		shopChanceSlider.MaxValue = 100;
		shopChanceSlider.Step = 1;
		shopChanceSlider.FocusMode = Control.FocusModeEnum.None;
		shopChanceSlider.Value = settings.ShopEnchantChancePercent;
		shopChanceLabel.Text = $"{settings.ShopEnchantChancePercent}%";
		shopChanceSlider.ValueChanged += v =>
		{
			settings.ShopEnchantChancePercent = Mathf.Clamp((int)v, 0, 100);
			shopChanceLabel.Text = $"{settings.ShopEnchantChancePercent}%";
			MoreEnchantSettingsStore.PersistCurrent();
		};
		root.AddChild(shopChanceRow);

		var ancientEnableRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var ancientCheck = new CheckBox
		{
			Text = "先古之民（Ancient）卡牌奖励随机附魔（不会出现诅咒档附魔）",
			ButtonPressed = settings.AncientRewardEnchantEnabled,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		ancientEnableRow.AddChild(ancientCheck);
		root.AddChild(ancientEnableRow);

		var ancientChanceRow = CreateLabeledRow(
			"先古之民卡牌奖励附魔概率（%）",
			out var ancientChanceSlider,
			out var ancientChanceLabel);
		ancientChanceSlider.MinValue = 0;
		ancientChanceSlider.MaxValue = 100;
		ancientChanceSlider.Step = 1;
		ancientChanceSlider.FocusMode = Control.FocusModeEnum.None;
		ancientChanceSlider.Value = settings.AncientRewardEnchantChancePercent;
		ancientChanceLabel.Text = $"{settings.AncientRewardEnchantChancePercent}%";
		ancientChanceSlider.ValueChanged += v =>
		{
			settings.AncientRewardEnchantChancePercent = Mathf.Clamp((int)v, 0, 100);
			ancientChanceLabel.Text = $"{settings.AncientRewardEnchantChancePercent}%";
			MoreEnchantSettingsStore.PersistCurrent();
		};
		root.AddChild(ancientChanceRow);

		var transformEnableRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var transformCheck = new CheckBox
		{
			Text = "变牌随机附魔（替牌继承后仍无附魔时）",
			ButtonPressed = settings.TransformEnchantEnabled,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		transformEnableRow.AddChild(transformCheck);
		root.AddChild(transformEnableRow);

		var transformChanceRow = CreateLabeledRow(
			"变牌附魔概率（%）",
			out var transformChanceSlider,
			out var transformChanceLabel);
		transformChanceSlider.MinValue = 0;
		transformChanceSlider.MaxValue = 100;
		transformChanceSlider.Step = 1;
		transformChanceSlider.FocusMode = Control.FocusModeEnum.None;
		transformChanceSlider.Value = settings.TransformEnchantChancePercent;
		transformChanceLabel.Text = $"{settings.TransformEnchantChancePercent}%";
		transformChanceSlider.ValueChanged += v =>
		{
			settings.TransformEnchantChancePercent = Mathf.Clamp((int)v, 0, 100);
			transformChanceLabel.Text = $"{settings.TransformEnchantChancePercent}%";
			MoreEnchantSettingsStore.PersistCurrent();
		};
		root.AddChild(transformChanceRow);

		var deckDirectEnableRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var deckDirectCheck = new CheckBox
		{
			Text = "直加牌组随机附魔（扭蛋额外打击/防御、涅奥苦痛、卷轴箱等；卷轴箱预览与入组同概率）",
			ButtonPressed = settings.DeckDirectEnchantEnabled,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		deckDirectEnableRow.AddChild(deckDirectCheck);
		root.AddChild(deckDirectEnableRow);

		var deckDirectChanceRow = CreateLabeledRow(
			"直加牌组附魔概率（%）",
			out var deckDirectChanceSlider,
			out var deckDirectChanceLabel);
		deckDirectChanceSlider.MinValue = 0;
		deckDirectChanceSlider.MaxValue = 100;
		deckDirectChanceSlider.Step = 1;
		deckDirectChanceSlider.FocusMode = Control.FocusModeEnum.None;
		deckDirectChanceSlider.Value = settings.DeckDirectEnchantChancePercent;
		deckDirectChanceLabel.Text = $"{settings.DeckDirectEnchantChancePercent}%";
		deckDirectChanceSlider.ValueChanged += v =>
		{
			settings.DeckDirectEnchantChancePercent = Mathf.Clamp((int)v, 0, 100);
			deckDirectChanceLabel.Text = $"{settings.DeckDirectEnchantChancePercent}%";
			MoreEnchantSettingsStore.PersistCurrent();
		};
		root.AddChild(deckDirectChanceRow);

		var combatEnableRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var combatCheck = new CheckBox
		{
			Text = "战斗内生成卡牌随机附魔",
			ButtonPressed = settings.CombatGeneratedEnchantEnabled,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		combatEnableRow.AddChild(combatCheck);
		root.AddChild(combatEnableRow);

		var combatChanceRow = CreateLabeledRow(
			"战斗内生成附魔概率（%）",
			out var combatChanceSlider,
			out var combatChanceLabel);
		combatChanceSlider.MinValue = 0;
		combatChanceSlider.MaxValue = 100;
		combatChanceSlider.Step = 1;
		combatChanceSlider.FocusMode = Control.FocusModeEnum.None;
		combatChanceSlider.Value = settings.CombatGeneratedEnchantChancePercent;
		combatChanceLabel.Text = $"{settings.CombatGeneratedEnchantChancePercent}%";
		combatChanceSlider.ValueChanged += v =>
		{
			settings.CombatGeneratedEnchantChancePercent = Mathf.Clamp((int)v, 0, 100);
			combatChanceLabel.Text = $"{settings.CombatGeneratedEnchantChancePercent}%";
			MoreEnchantSettingsStore.PersistCurrent();
		};
		root.AddChild(combatChanceRow);

		void RefreshShopAncientCombatUi()
		{
			var shopOn = settings.ShopEnchantEnabled;
			shopChanceSlider.MouseFilter = shopOn ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
			shopChanceSlider.Modulate = shopOn ? Colors.White : new Color(1f, 1f, 1f, 0.45f);

			var ancOn = settings.AncientRewardEnchantEnabled;
			ancientChanceSlider.MouseFilter = ancOn ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
			ancientChanceSlider.Modulate = ancOn ? Colors.White : new Color(1f, 1f, 1f, 0.45f);

			var transformOn = settings.TransformEnchantEnabled;
			transformChanceSlider.MouseFilter = transformOn ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
			transformChanceSlider.Modulate = transformOn ? Colors.White : new Color(1f, 1f, 1f, 0.45f);

			var deckDirectOn = settings.DeckDirectEnchantEnabled;
			deckDirectChanceSlider.MouseFilter = deckDirectOn ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
			deckDirectChanceSlider.Modulate = deckDirectOn ? Colors.White : new Color(1f, 1f, 1f, 0.45f);

			var combatOn = settings.CombatGeneratedEnchantEnabled;
			combatChanceSlider.MouseFilter = combatOn ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
			combatChanceSlider.Modulate = combatOn ? Colors.White : new Color(1f, 1f, 1f, 0.45f);
		}

		shopCheck.Toggled += pressed =>
		{
			settings.ShopEnchantEnabled = pressed;
			RefreshShopAncientCombatUi();
			MoreEnchantSettingsStore.PersistCurrent();
		};
		ancientCheck.Toggled += pressed =>
		{
			settings.AncientRewardEnchantEnabled = pressed;
			RefreshShopAncientCombatUi();
			MoreEnchantSettingsStore.PersistCurrent();
		};
		combatCheck.Toggled += pressed =>
		{
			settings.CombatGeneratedEnchantEnabled = pressed;
			RefreshShopAncientCombatUi();
			MoreEnchantSettingsStore.PersistCurrent();
		};
		transformCheck.Toggled += pressed =>
		{
			settings.TransformEnchantEnabled = pressed;
			RefreshShopAncientCombatUi();
			MoreEnchantSettingsStore.PersistCurrent();
		};
		deckDirectCheck.Toggled += pressed =>
		{
			settings.DeckDirectEnchantEnabled = pressed;
			RefreshShopAncientCombatUi();
			MoreEnchantSettingsStore.PersistCurrent();
		};
		betaEnchantCheck.Toggled += pressed =>
		{
			settings.BetaRewardEnchantmentsEnabled = pressed;
			MoreEnchantSettingsStore.PersistCurrent();
		};
		RefreshShopAncientCombatUi();

		var chimeraRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		var chimeraCheck = new CheckBox
		{
			Text = "按卡牌稀有度使用奇美拉式五档权重",
			ButtonPressed = settings.UseChimeraRarityByCardRarity,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		chimeraRow.AddChild(chimeraCheck);
		root.AddChild(chimeraRow);

		var weightsHint = new Label
		{
			Text = "关闭上一项时，使用下列相对权重（与五档一同归一化）：",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		root.AddChild(weightsHint);

		var weightsGrid = new GridContainer { Columns = 2, MouseFilter = Control.MouseFilterEnum.Pass };
		weightsGrid.AddThemeConstantOverride("h_separation", 12);
		weightsGrid.AddThemeConstantOverride("v_separation", 6);

		var wCommon = AddWeightSpin(weightsGrid, "普通", settings.WeightCommon, v => settings.WeightCommon = v);
		var wUncommon = AddWeightSpin(weightsGrid, "罕见", settings.WeightUncommon, v => settings.WeightUncommon = v);
		var wCurse = AddWeightSpin(weightsGrid, "诅咒", settings.WeightCurse, v => settings.WeightCurse = v);
		var wRare = AddWeightSpin(weightsGrid, "稀有", settings.WeightRare, v => settings.WeightRare = v);
		var wSpecial = AddWeightSpin(weightsGrid, "特殊", settings.WeightSpecial, v => settings.WeightSpecial = v);
		root.AddChild(weightsGrid);

		void UpdateWeightsUi()
		{
			var on = settings.UseChimeraRarityByCardRarity;
			weightsHint.Visible = !on;
			weightsGrid.Visible = !on;
			foreach (var c in new[] { wCommon, wUncommon, wCurse, wRare, wSpecial })
				c.Editable = !on;
		}

		chimeraCheck.Toggled += pressed =>
		{
			settings.UseChimeraRarityByCardRarity = pressed;
			UpdateWeightsUi();
			MoreEnchantSettingsStore.PersistCurrent();
		};

		UpdateWeightsUi();

		content.AddChild(root);
		RefreshSizeMethod?.Invoke(__instance, null);
	}

	private static Control CreateDivider()
	{
		var rect = new ColorRect
		{
			CustomMinimumSize = new Vector2(0, 1),
			Color = new Color(0.35f, 0.35f, 0.38f, 0.9f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		return rect;
	}

	private static Label CreateSectionTitle(string text)
	{
		return new Label
		{
			Text = text,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
	}

	private static HBoxContainer CreateLabeledRow(string labelText, out HSlider slider, out Label valueLabel)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		row.AddThemeConstantOverride("separation", 12);

		var label = new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(280, 0),
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		slider = new HSlider { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		valueLabel = new Label
		{
			CustomMinimumSize = new Vector2(48, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		row.AddChild(label);
		row.AddChild(slider);
		row.AddChild(valueLabel);
		return row;
	}

	private static SpinBox AddWeightSpin(
		GridContainer grid,
		string name,
		int initial,
		Action<int> apply)
	{
		var lbl = new Label { Text = name, MouseFilter = Control.MouseFilterEnum.Ignore };
		grid.AddChild(lbl);

		var spin = new SpinBox
		{
			MinValue = 1,
			MaxValue = 1_000_000,
			Value = Math.Max(1, initial),
			FocusMode = Control.FocusModeEnum.None,
		};
		spin.ValueChanged += v =>
		{
			apply(Math.Max(1, (int)v));
			MoreEnchantSettingsStore.PersistCurrent();
		};
		grid.AddChild(spin);
		return spin;
	}
}
