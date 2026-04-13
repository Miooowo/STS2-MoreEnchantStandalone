namespace MoreEnchant;

/// <summary>旧版 JSON 缺字段时的内存修补；本地加载后若变更则写回磁盘，联机房主快照仅内存修补。</summary>
internal static class MoreEnchantSettingsMigration
{
	/// <returns>是否有字段被修改（本地应据此 <see cref="MoreEnchantSettingsStore.PersistCurrent"/>）。</returns>
	internal static bool Apply(MoreEnchantSettings s)
	{
		var changed = false;

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

		return changed;
	}
}
