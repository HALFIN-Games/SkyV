# Release context

Repo: `HALFIN-Games/SkyV`

This repo is the home for versioned tester-kit releases (and later the loader).

## What is tracked in Git

- Root docs/scripts (`README.md`, `RELEASE_CONTEXT.md`, `docs/`, `scripts/`)
- Per-kit documentation under `testing-kits/<kit>/README.md` and `testing-kits/<kit>/docs/`

## What is NOT tracked in Git

- The full client payload inside each kit: `testing-kits/**/client/`
- Built archives: `testing-kits/*.zip` and checksums

Those are published as GitHub Release assets.

## Current kit versions

- `v1_skyrim-1.6.1170_skse-2.2.6_skymp-aa22bf2d`
- `v2_skyrim-1.6.1170_skse-2.2.6_skymp-skyv-clienthooks`
  - Adds `Data\\Platform\\Plugins\\skyv-client-hooks.js` to force starter outfit visuals during/after RaceMenu.
- `v3_skyrim-1.6.1170_skse-2.2.6_skymp-skyv-clienthooks-autoload`
  - Ensures `skymp5-client.js` auto-loads `skyv-client-hooks.js`.

## How to publish a kit

See `docs/RELEASING_TEST_KITS.md`.

## When making v3+

- Start by copying the previous kit folder name pattern (new `v3_...`).
- Replace the `client/` payload with the new build output.
- Ensure any extra SkyV plugins are present in `client\\Data\\Platform\\Plugins\\`.
- Update the kit `README.md` (baseline, server connect instructions, and what changed).
- Zip + checksum + publish as a Release.
