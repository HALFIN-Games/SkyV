## Testing kits

This folder holds zip-ready “tester kits” for multi-machine validation.

The full client payload under each kit (especially `client/`) is intentionally not tracked by Git.
Versioned kit zips are published via GitHub Releases instead.

Create a new version folder per baseline, for example:

- `testing-kits\v1_skyrim-1.6.1170_skse-2.2.6_skymp-aa22bf2d`

Latest:

- `testing-kits\v5_skyrim-1.6.1170_skse-2.2.6_skymp-skyv-clienthooks-fixed-sp`

Each version folder should contain:

- `client\Data\` (SkyMP client payload)
- `docs\` (run guides + remote tester guide)
- `README.md` (one-page instructions for the tester)

To publish a kit as a release, see `docs/RELEASING_TEST_KITS.md`.
