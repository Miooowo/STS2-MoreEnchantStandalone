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

		if (s.SchemaVersion < 6)
		{
			s.RewardEnchantOnlyFilter ??= "";
			s.BlacklistedEnchantmentIds ??= [];
			s.SchemaVersion = 6;
			changed = true;
		}

		s.RewardEnchantOnlyFilter ??= "";
		if (s.BlacklistedEnchantmentIds == null)
		{
			s.BlacklistedEnchantmentIds = [];
			changed = true;
		}
		else
		{
			var before = s.BlacklistedEnchantmentIds.Count;
			s.BlacklistedEnchantmentIds = s.BlacklistedEnchantmentIds
				.Where(static id => !string.IsNullOrWhiteSpace(id))
				.Select(static id => id.Trim().ToUpperInvariant())
				.Distinct(StringComparer.Ordinal)
				.ToList();
			if (s.BlacklistedEnchantmentIds.Count != before)
				changed = true;
		}

		var rewardChance = Math.Clamp(s.RewardEnchantChancePercent, 0, 100);
		if (rewardChance != s.RewardEnchantChancePercent)
		{
			s.RewardEnchantChancePercent = rewardChance;
			changed = true;
		}

		var shopChance = Math.Clamp(s.ShopEnchantChancePercent, 0, 100);
		if (shopChance != s.ShopEnchantChancePercent)
		{
			s.ShopEnchantChancePercent = shopChance;
			changed = true;
		}

		var ancientChance = Math.Clamp(s.AncientRewardEnchantChancePercent, 0, 100);
		if (ancientChance != s.AncientRewardEnchantChancePercent)
		{
			s.AncientRewardEnchantChancePercent = ancientChance;
			changed = true;
		}

		var combatChance = Math.Clamp(s.CombatGeneratedEnchantChancePercent, 0, 100);
		if (combatChance != s.CombatGeneratedEnchantChancePercent)
		{
			s.CombatGeneratedEnchantChancePercent = combatChance;
			changed = true;
		}

		var transformChance = Math.Clamp(s.TransformEnchantChancePercent, 0, 100);
		if (transformChance != s.TransformEnchantChancePercent)
		{
			s.TransformEnchantChancePercent = transformChance;
			changed = true;
		}

		var deckDirectChance = Math.Clamp(s.DeckDirectEnchantChancePercent, 0, 100);
		if (deckDirectChance != s.DeckDirectEnchantChancePercent)
		{
			s.DeckDirectEnchantChancePercent = deckDirectChance;
			changed = true;
		}

		var startingDeckChance = Math.Clamp(s.StartingDeckEnchantChancePercent, 0, 100);
		if (startingDeckChance != s.StartingDeckEnchantChancePercent)
		{
			s.StartingDeckEnchantChancePercent = startingDeckChance;
			changed = true;
		}

		var weightCommon = Math.Clamp(s.WeightCommon, 0, 2000);
		if (weightCommon != s.WeightCommon)
		{
			s.WeightCommon = weightCommon;
			changed = true;
		}

		var weightUncommon = Math.Clamp(s.WeightUncommon, 0, 2000);
		if (weightUncommon != s.WeightUncommon)
		{
			s.WeightUncommon = weightUncommon;
			changed = true;
		}

		var weightCurse = Math.Clamp(s.WeightCurse, 0, 2000);
		if (weightCurse != s.WeightCurse)
		{
			s.WeightCurse = weightCurse;
			changed = true;
		}

		var weightRare = Math.Clamp(s.WeightRare, 0, 2000);
		if (weightRare != s.WeightRare)
		{
			s.WeightRare = weightRare;
			changed = true;
		}

		var weightSpecial = Math.Clamp(s.WeightSpecial, 0, 2000);
		if (weightSpecial != s.WeightSpecial)
		{
			s.WeightSpecial = weightSpecial;
			changed = true;
		}

		return changed;
	}
}
