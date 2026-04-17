# SkyV

This repository contains the SkyV Windows launcher (the “loader”).

The launcher is responsible for:

- opening from the website `Join Server` button (`skyv://...`)
- showing queue status (nice UX)
- ensuring required files are present (SKSE + server-required pack)
- launching Skyrim only when admitted

Reference specs live in `Vokun-Project/docs/skyv/`:

- `LAUNCHER_V0_SPEC.md`
- `CONTRACTS.md`

## Legacy (tester kits)

Early development used “manual tester kits” as a temporary distribution approach.

Those files are kept for reference under:

- `legacy/testing-kits/`
- `legacy/scripts/`
- `legacy/docs/`

## Building Vokun WL (CLI)

Prereqs:

- .NET SDK 8
- Windows 10/11 SDK (for MSIX packaging tools)

Run launcher (Release):

```powershell
cd C:\Users\t\Documents\Github\SkyV
dotnet build .\SkyV.sln -c Release
dotnet run --project .\src\SkyV.Launcher\SkyV.Launcher.csproj -c Release
```

Create test signing cert + build MSIX:

```powershell
cd C:\Users\t\Documents\Github\SkyV
.\scripts\New-VokunWLTestCert.ps1
.\scripts\Build-VokunWLMSIX.ps1 -Version 0.1.0.0
```

## Tester install (machine-wide)

Release artifacts:

- `VokunWL_<version>.msix`
- `VokunWL.msix`
- `VokunWL_TestCert.cer`
- `VokunWLInstaller_<version>.exe`
- `VokunWLInstaller.exe`
- `Install-VokunWLTester.ps1` (fallback)

Install (recommended):

```powershell
.\VokunWLInstaller.exe
```

Prepare a GitHub Release folder:

```powershell
.\scripts\Prepare-VokunWLRelease.ps1 -Version 0.1.0.0
```
