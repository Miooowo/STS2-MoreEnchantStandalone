# MoreEnchantStandalone 0.10.0

## Highlights
- Added two new `AMALGAMATOR` event choices:
  - Inject Strikes: remove 2 Strikes, then enchant one Attack with `Ultimate Strike`.
  - Inject Defends: remove 2 Defends, then enchant one Block Skill with `Ultimate Defend`.
- Added event-exclusive enchantments:
  - `Ultimate Strike` (+14 play damage)
  - `Ultimate Defend` (+11 play block)
  These do not enter random enchant pools.
- Removed `Neutral` enchantment registration/localization so it no longer appears.
- Rebalanced `Gem`: now grants replay 2 only on the first play each combat.
- Reworked `Snakebite`: base poison is 7, upgraded poison is 10; upgraded Snakebite cards no longer reduce cost.
- Fixed Snakebite upgrade preview: no cost reduction is shown in preview, and poison preview reflects 7 -> 10.
- Fully removed `NeutralWeakEnchantment` code and added compendium title-localization safeguards to prevent localization-key crashes.
- Renamed Gem enchantment model id to `GemEnchantment` and updated localization keys accordingly.
- Promoted `imbued` and `instinct` to Special rarity (with `tezcataras_ember`, `goopy`, `clone`, `glam`).
- Restricted `royally_approved` to shop-only random enchant source.

## Compatibility / Build
- Updated 104+ callback signature handling for `AfterCardGeneratedForCombat`.
- Version bumped to `0.10.0`.
