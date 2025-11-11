# VigilModUpdater

A MelonLoader plugin for the game Vigil that automatically checks for and installs updates for mods from GitHub releases.

## For Mod Developers

To make your mod compatible with VigilModUpdater:

1. **Host your mod on GitHub** with releases
2. **Set the DownloadLink** in your mod's MelonInfo to your GitHub repository URL:

```csharp
[assembly: MelonInfo(typeof(YourMod), "YourModName", "1.0.0", "YourAuthor", "https://github.com/yourusername/yourmod")]
```

3. **Create GitHub releases** with semantic versioning (e.g., v1.0.0) and include your mod DLL in the release assets

## How It Works

1. Scans loaded mods for GitHub DownloadLink
2. Checks latest GitHub release for newer versions
3. Downloads and caches updates
4. Creates backups of existing files
5. Restarts game to apply updates

## Installation

Place the latest `VigilModUpdater.dll` in releases in your `Plugins` folder.