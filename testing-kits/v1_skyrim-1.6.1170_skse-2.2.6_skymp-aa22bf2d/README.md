## SkyV tester kit (v1)

Baseline:

- Skyrim runtime: `1.6.1170`
- SKSE: `2.2.6`
- SkyMP upstream commit: `aa22bf2d`

This kit is designed for a clean tester machine to connect to a SkyMP server.

---

## A) Tester prerequisites

- Windows 10/11 x64
- Steam + “Skyrim Special Edition” installed
- Skyrim runtime is `1.6.1170`
- SKSE `2.2.6` installed into Skyrim root
- Tailscale installed (only needed when you are ready to connect remotely)

---

## B) Install steps (tester)

1) Install Skyrim SE (Steam), launch once, then quit.

2) Install SKSE `2.2.6` for runtime `1.6.1170`.

Skyrim root folder (contains `SkyrimSE.exe`):

- `C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\`

Copy these from the SKSE download into the Skyrim root folder:

- `skse64_loader.exe`
- `skse64_1_6_1170.dll`

Then copy scripts:

- From SKSE: `Data\Scripts\*.pex`
- To Skyrim: `...\Skyrim Special Edition\Data\Scripts\`

Verify SKSE logs exist after one launch:

- `%USERPROFILE%\Documents\My Games\Skyrim Special Edition\SKSE\`

3) Install the SkyMP client payload from this kit.

- Copy everything from `client\Data\` into:
  - `C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\Data\`

4) Ensure only the SkyMP plugins are enabled.

In `...\Skyrim Special Edition\Data\SKSE\Plugins\`, keep only:

- `SkyrimPlatform.dll`
- `MpClientPlugin.dll`

5) Launch the game via:

- `C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\skse64_loader.exe`

6) Point the client at the server.

The client reads its connection target from:

- `...\Skyrim Special Edition\Data\Platform\Plugins\skymp5-client-settings.txt`

This kit ships with a default server target. If you need to change it, edit the JSON keys:

- `server-ip`
- `server-port`

Example (VPS):

```json
{
  "server-ip": "16.16.122.192",
  "server-port": 7777,
  "server-info-ignore": true
}
```

---

## C) Connect (remote)

This server may be hosted on a VPS (public IP) or on a home PC via a VPN overlay.

- VPS: set `server-ip` to the VPS public IPv4 (or DNS), `server-port` to `7777`.
- VPN overlay (optional): set `server-ip` to the host’s VPN IP (e.g., a Tailscale `100.x.y.z`).

---

## D) Included docs

See `docs\`:

- `SKYMP_LOCAL_RUN_GUIDE.md`
- `TESTER_SETUP_GUIDE.md`
- `BASELINE_SNAPSHOT.md`
