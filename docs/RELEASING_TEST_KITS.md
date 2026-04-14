# Releasing tester kits

This repo treats the full tester kit payload (especially `client/`) as a local-only artifact and publishes it via GitHub Releases.

## Prerequisites

- GitHub CLI installed: `gh`
- Authenticated: `gh auth login`

## Publish a kit

1) Ensure the kit folder exists under `testing-kits/` (example: `testing-kits/v2_skyrim-1.6.1170_skse-2.2.6_skymp-skyv-clienthooks/`).

2) From the repo root, run:

```powershell
./scripts/Publish-TestKit.ps1 -KitFolder "testing-kits/v2_skyrim-1.6.1170_skse-2.2.6_skymp-skyv-clienthooks"
```

This will:

- Create `testing-kits/<folder>.zip`
- Create `testing-kits/<folder>.zip.sha256`
- Create (or update) a GitHub Release tagged `kit-<folder>`

