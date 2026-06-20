## Fixed
- Fixed a multiplayer checksum divergence issue in temporary Hextech rune integration.
- Temporary rune grant/remove flows now stay on the main async context (removed `ConfigureAwait(false)`), preventing one-side-only relic state drift (for example, lingering `COSPLAY_RUNE`).
- Fixed `Clumsy` incorrectly triggering from teammate card plays in multiplayer; it now only triggers when the cursed card owner plays another card.
- Fixed `Demon Shield` teammate block transfer desync in multiplayer by using a deterministic ally target path instead of per-client local targeting UI.
