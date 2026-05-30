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
- Fixed a multiplayer desync on exiting `EVENT.NEOW` where transform-enchant logic consumed `Transformations` RNG only on local client, causing checksum divergence.
- Fixed a multiplayer combat desync where combat-generated card enchantment ran only on local client (e.g. `MASTER_OF_STRATEGY`), causing card-state mismatch and `Transformations` RNG divergence.
- Fixed a potential multiplayer RNG desync in direct-to-deck random enchant flows (events/relics/scroll-box style paths) by enforcing deterministic execution on all clients.
- Fixed cross-client hook-argument visibility edge cases in `AfterCardGeneratedForCombat` by falling back to `card.Owner is Player`, preventing one side from skipping generated-card enchant logic.
- Fixed owner-resolution edge cases for generated cards in multiplayer by using `creator(Player)` as the primary RNG owner and `card.Owner` as fallback, preventing start-of-turn generated-card desyncs (e.g. enchanted `SWORD_SAGE` only appearing on one side).
- Fixed `Soul_Detachment` cleanup for segmented/replacement enemy edge cases (e.g. centipede): souls are now removed when the linked body is not alive or has left the current combat state, not only when `IsDead` is true.
- Fixed centipede soul persistence caused by inherited `ReattachPower` on soul clones: `Soul_Detachment` now strips `ReattachPower` immediately after spawning the soul.
- Changed `Soul_Detachment` soul clones into pure training dummies by clearing all inherited powers after spawn; only the soul-link and forced-stun control remain.

## Build
- Version bumped to `0.11.0`, Release artifacts export to `pre-release/`.
