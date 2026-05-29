## Added
- Added rare enchantment `Soul_Detachment` (`灵魂抽离`).
- First play each combat now detaches the target's Soul for attack cards that can deal play damage.
- Added soul-link behavior: damage dealt to the Soul is mirrored to the original body (one-way), with recursion guard.

## Changed
- Added low-opacity Soul visuals and persistent stun maintenance in combat.
- Added new localization keys `SOUL_DETACHMENT_ENCHANTMENT.*` in both English and Chinese.

## Fixed
- Fixed mutable-model cloning error when summoning Soul clones from combat monsters.
- Hardened persistent-stun timing to reduce end-of-combat edge-case side effects.
- Fixed segmented-enemy edge case where Soul could remain after body death and lose stunned intent.
- Added end-run safety guards for null `ModelId` in progress/death-quote paths to prevent GameOver crashes.

## Build
- Version bumped to `0.11.0`, Release artifacts export to `pre-release/`.
