# MoreEnchantStandalone 0.10.0

## Highlights
- Added two new `AMALGAMATOR` event choices:
  - Inject Strikes: remove 2 Strikes, then enchant one Attack with `Ultimate Strike`.
  - Inject Defends: remove 2 Defends, then enchant one Block Skill with `Ultimate Defend`.
- Added event-exclusive enchantments:
  - `Ultimate Strike` (play damage fixed to 14)
  - `Ultimate Defend` (play block fixed to 11)
  These do not enter random enchant pools.
- Rebalanced `Gem`: now grants replay 2 only on the first play each combat.
- Reworked `Snakebite`: base poison is 7, upgraded poison is 10; upgraded Snakebite cards no longer reduce cost.
- Promoted `imbued` and `instinct` to Special rarity (with `tezcataras_ember`, `goopy`, `clone`, `glam`).
- Restricted `royally_approved` to shop-only random enchant source.

## Compatibility / Build
- Updated 104+ callback signature handling for `AfterCardGeneratedForCombat`.
- Version bumped to `0.10.0`.
