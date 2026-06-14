# Ultimate Fortnite Modding Tool

A modding tool for importing the latest Fortnite assets to older Fortnite builds.

## Features

- **Automated mesh conversion** — Converts exported .psk mesh files to .fbx format automatically using Blender
- **Automatic texture assignment** — Connects exported textures with their corresponding materials, with manual override options if needed
- **Real-time preview rendering** — Render a preview of your skin before export (Note: The preview won't be 100% accurate since Blender is a different rendering engine than Unreal Engine)
- **Automatic asset generation** — Automatically generates all required Fortnite cosmetic assets (CID, HID, HS, CPs) and modifies them for game compatibility
- **AssetRegistry generation** — Creates the AssetRegistry.bin file so Fortnite recognizes your custom skin
- **Official engine support** — Compatible with standard Unreal Engine 4.26.2 (no modded version required)
- **Fast workflow** — Backport a custom skin in under 1 minute

## Requirements

Before using this tool, you need to install and configure:

- **Windows OS** (tested on Windows 10/11)
- **.NET Framework** (version required by your system)
- **Blender 4.5** — [Download here](https://www.blender.org/download/)
  - Install these plugins via: Edit → Preferences → Add-ons → Install from File, select the plugin, and enable it
  - **PSA/PSK Importer Plugin** — [Download here](https://extensions.blender.org/download/sha256:9301a57466e3d41907b4b3175a7cc0e5df80aaefcc594676e5b898fdf13e7ad2/add-on-io-scene-psk-psa-v8.2.4.zip?repository=%2Fapi%2Fv1%2Fextensions%2F&blender_version_min=4.4.0&blender_version_max=5.0.0)
  - **Better FBX Exporter** — [Download here](https://github.com/notzyron/Ultimate-Fortnite-Modding-Tool/releases/download/v1.2.0/better_fbx_blender4.5.zip)
- **Unreal Engine 4.26.2** — [Download from Epic Games Launcher](https://www.epicgames.com/store/en-US/download) (Note: it may display as "Unreal Engine 4.26" in the launcher). Open Unreal Engine, create a new project, then go to Edit → Plugins and enable:
  - `Python Editor Script Plugin` (built-in)
  - `Editor Scripting Utilities` (built-in)
## Installation

### Option 1: Use the Compiled .EXE (Recommended)
1. Download the latest `.exe` from [Releases](https://github.com/notzyron/Ultimate-Fortnite-Modding-Tool/releases)
2. Run the `.exe`
3. **Configure settings** (first launch):
   - Go to Settings page
   - Set **Blender executable path** (e.g., `C:\Program Files\Blender Foundation\Blender 4.5\blender.exe`)
   - Set **UE executable path** (e.g., `C:\Program Files\Epic Games\UE_4.26\Engine\Binaries\Win64\UE4Editor.exe`)
   - Set **UE project path** (path to your Fortnite UE 4.26 project)
   - Select **UE version** (Original UE or modded version for Fortnite modding)
   - Select **FN version**
   - Save settings

### Option 2: Build from Source
1. Clone or download this repository
2. Open `UFMT.sln` in Visual Studio
3. Build the solution (Build → Build Solution)
4. Run the application
5. Follow the settings configuration steps above

## Usage

### Setup

1. Create a folder anywhere on your pc, that will contain your skin folders
2. Click **"Create Skin Folder"** and enter your skin's codename (e.g., `QuarterClaspZoom` from the Character ID)
3. Place your exported .psk meshes in `[skin codename]/Source/Meshes/[mesh character part type]` (eg. if your mesh is a body, place it in [skin codename]/Source/Meshes/[Body])
4. Place your exported .psa lobby pose in `[skin codename]/Source/Lobby_Animation`
5. Place textures and icons in `[skin codename]/Source/Textures/`

### Configure & Preview

1. Specify your skin folder path in **"Current Skin Path"**
2. The program auto-detects materials and assigns textures (may have issues with reskins)
3. Manually adjust texture assignments if needed using the dropdowns
4. If the skin has a skin boost color and exponent, enable the option in the material dropdowns and enter the option's red, green, blue and alpha values
5. Enter skin details: **name**, **description**, **rarity**, **gender**
6. Click **"Render"** to preview the skin
7. If the skin looks too shiny, enable **"Swizzle Roughness to Green"** (This usually happens on skins that were made after chapter 4, due to Engine version switch)

### Export & Deploy

1. Click **"Export"**
2. The program will:
   - Convert .psk files to .fbx in Blender
   - Import meshes, textures and animations into Unreal Engine and apply correct settings to them
   - Generate all required cosmetic files
   - Create AssetRegistry.bin for game recognition
3. Your finished skin will be in `[skin codename]/[Fortnite Version]/Output/FortniteGame/` (eg. If you're doing the skin for Fortnite 13.40, the FortniteGame folder will be in `[skin codename]/13.40/Output/FortniteGame/`
4. Pak the output folder with u4pak, move the pak to your Fortnite build Paks folder, copy any .sig files from that folder and rename it the same as your custom .pak, then launch the game!

**Note:** The export process may take a minute as Blender and UE scripts run in the background (export time varies depending on PC specs; typical systems take 15-30 seconds). Wait until the console displays "Your custom skin is ready! Check the output folder"

## Compatibility

- **Unreal Engine:** 4.26 (original or modded for Fortnite)
- **Fortnite Versions:** v14.30, v13.40
- **Blender:** 4.5

## Credits
- **[UAssetAPI](https://github.com/atenfyr/UAssetAPI)** — Asset handling and parsing library
- **Win3** — UI framework
- **[QueenIO](https://github.com/Code-Vein-Tool-Hub/QueenIO)** — Asset extraction techniques (adapted code with attribution)
- **[AssetRegistryInjector](https://github.com/Code-Vein-Tool-Hub/AssetRegistryTool)** — Registry injection utilities (adapted code with attribution)
- **[Zylox](https://github.com/zyloxmods)** — BetterFBX exporter plugin

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## Troubleshooting

- **Plugins not found in UE:** Ensure Python Script Editor and Scripting Utilities are enabled in Edit → Plugins
- **Blender scripts failing:** Verify Python is accessible from your Blender installation
- **Import stuck:** Check that all paths in Settings are correct and files exist

---

**Have questions or found a bug?** Open an issue on GitHub!
