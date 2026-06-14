namespace MoreEnchant;

/// <summary>旧版 JSON 缺字段时的内存修补；本地加载后若变更则写回磁盘，联机房主快照仅内存修补。</summary>
internal static class MoreEnchantSettingsMigration
{
	/// <returns>是否有字段被修改（本地应据此 <see cref="MoreEnchantSettingsStore.PersistCurrent"/>）。</returns>
	internal static bool Apply(MoreEnchantSettings s)
	{
		var changed = false;
		var defaultSettings = new MoreEnchantSettings();

		if (s.WeightCurse <= 0)
		{
			s.WeightCurse = 250;
			changed = true;
		}

		if (s.SchemaVersion < 2)
		{
			s.TransformEnchantEnabled = true;
			s.TransformEnchantChancePercent = s.RewardEnchantChancePercent > 0
				? Math.Clamp(s.RewardEnchantChancePercent, 0, 100)
				: 10;
			s.SchemaVersion = 2;
			changed = true;
		}

		if (s.SchemaVersion < 3)
		{
			s.DeckDirectEnchantEnabled = true;
			s.DeckDirectEnchantChancePercent = 10;
			s.SchemaVersion = 3;
			changed = true;
		}

		if (s.SchemaVersion < 4)
		{
			s.BetaRewardEnchantmentsEnabled = false;
			s.SchemaVersion = 4;
			changed = true;
		}

		if (s.SchemaVersion < 5)
		{
			s.StartingDeckEnchantEnabled = false;
			s.StartingDeckEnchantChancePercent = 10;
			s.SchemaVersion = 5;
			changed = true;
		}

		// 设置已移除：统一锁定为默认概率，并开启所有附魔路径。
		if (s.RewardEnchantChancePercent != defaultSettings.RewardEnchantChancePercent)
		{
			s.RewardEnchantChancePercent = defaultSettings.RewardEnchantChancePercent;
			changed = true;
		}
		if (s.ShopEnchantChancePercent != defaultSettings.ShopEnchantChancePercent)
		{
			s.ShopEnchantChancePercent = defaultSettings.ShopEnchantChancePercent;
			changed = true;
		}
		if (s.AncientRewardEnchantChancePercent != defaultSettings.AncientRewardEnchantChancePercent)
		{
			s.AncientRewardEnchantChancePercent = defaultSettings.AncientRewardEnchantChancePercent;
			changed = true;
		}
		if (s.CombatGeneratedEnchantChancePercent != defaultSettings.CombatGeneratedEnchantChancePercent)
		{
			s.CombatGeneratedEnchantChancePercent = defaultSettings.CombatGeneratedEnchantChancePercent;
			changed = true;
		}
		if (s.TransformEnchantChancePercent != defaultSettings.TransformEnchantChancePercent)
		{
			s.TransformEnchantChancePercent = defaultSettings.TransformEnchantChancePercent;
			changed = true;
		}
		if (s.DeckDirectEnchantChancePercent != defaultSettings.DeckDirectEnchantChancePercent)
		{
			s.DeckDirectEnchantChancePercent = defaultSettings.DeckDirectEnchantChancePercent;
			changed = true;
		}
		if (s.StartingDeckEnchantChancePercent != defaultSettings.StartingDeckEnchantChancePercent)
		{
			s.StartingDeckEnchantChancePercent = defaultSettings.StartingDeckEnchantChancePercent;
			changed = true;
		}
		if (!s.ShopEnchantEnabled)
		{
			s.ShopEnchantEnabled = true;
			changed = true;
		}
		if (!s.AncientRewardEnchantEnabled)
		{
			s.AncientRewardEnchantEnabled = true;
			changed = true;
		}
		if (!s.CombatGeneratedEnchantEnabled)
		{
			s.CombatGeneratedEnchantEnabled = true;
			changed = true;
		}
		if (!s.TransformEnchantEnabled)
		{
			s.TransformEnchantEnabled = true;
			changed = true;
		}
		if (!s.DeckDirectEnchantEnabled)
		{
			s.DeckDirectEnchantEnabled = true;
			changed = true;
		}
		if (!s.StartingDeckEnchantEnabled)
		{
			s.StartingDeckEnchantEnabled = true;
			changed = true;
		}
		if (!s.BetaRewardEnchantmentsEnabled)
		{
			s.BetaRewardEnchantmentsEnabled = true;
			changed = true;
		}
		if (s.UseChimeraRarityByCardRarity != defaultSettings.UseChimeraRarityByCardRarity)
		{
			s.UseChimeraRarityByCardRarity = defaultSettings.UseChimeraRarityByCardRarity;
			changed = true;
		}
		if (s.WeightCommon != defaultSettings.WeightCommon)
		{
			s.WeightCommon = defaultSettings.WeightCommon;
			changed = true;
		}
		if (s.WeightUncommon != defaultSettings.WeightUncommon)
		{
			s.WeightUncommon = defaultSettings.WeightUncommon;
			changed = true;
		}
		if (s.WeightCurse != defaultSettings.WeightCurse)
		{
			s.WeightCurse = defaultSettings.WeightCurse;
			changed = true;
		}
		if (s.WeightRare != defaultSettings.WeightRare)
		{
			s.WeightRare = defaultSettings.WeightRare;
			changed = true;
		}
		if (s.WeightSpecial != defaultSettings.WeightSpecial)
		{
			s.WeightSpecial = defaultSettings.WeightSpecial;
			changed = true;
		}

		return changed;
	}
}
