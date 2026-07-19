发布日期：2026-07-08

## Added
- Seven new enchantment icons: Charge Up, Frost Channel, Synthesis, Micro Current, Initiative, Spectral, and Like Dagger (thanks to Penguin).
- RitsuLib settings: “Allow only these enchantments” whitelist (match by ID / English / Chinese title) and per-enchantment blacklist toggles for the random pool.

## Changed
- In Simplified Chinese, enchantments without a dedicated icon now use the enchanted book fallback (`enchanted_book.png` texture exported from `enchanted_book.gif`; Godot cannot import GIF directly).

## Fixed
- Adapted to `Hook.BeforeSideTurnEnd` / `Hook.AfterSideTurnEnd` (replacing removed `BeforeTurnEnd` / `AfterTurnEnd`) so the mod loads on the latest game build.
- Aligned `CreatureCmd.Damage` and `AttackCommand.FromCard` call sites with the new `CardModel` + `CardPlay?` signatures.
- Migrated card play destination overrides to `ModifyCardPlayResultLocation` (`CardLocation`).
