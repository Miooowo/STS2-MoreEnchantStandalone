发布日期：2026-06-26

## Changed
- Reordered RitsuLib settings so Reward Enchant Chance appears first and Starting Deck options are shown second.
- Added custom rarity-weight sliders (Common/Uncommon/Curse/Rare/Special), editable only when default rarity weighting is disabled.

## Fixed
- Localized MoreEnchant RitsuLib settings labels/descriptions via localization keys (English + Chinese) instead of hardcoded text.
- Prevented settings migration from overwriting player-tuned values; migration now clamps only invalid ranges.
- Fixed enchant chance settings not taking effect by making all enchant entry points read effective runtime settings (including host-synced multiplayer values and beta-pool toggles).

