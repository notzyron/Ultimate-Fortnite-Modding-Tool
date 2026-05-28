# Ultimate Fortnite Modding Tool

A modding tool for importing the latest Fortnite assets to older Fortnite builds.

## Features

- **Automated mesh conversion** — Converts exported .psk mesh files to .fbx format automatically using Blender
- **Intelligent material setup** — Automatically pairs textures (diffuse, normal, mask, specular) with their corresponding materials, with manual override options
- **Real-time preview rendering** — Render a preview of your skin before export (Note: The preview won't be 100% accurate since Blender is a different rendering engine than Unreal Engine)
- **One-click asset generation** — Automatically generates all required Fortnite cosmetic assets (CID, HID, HS, CPs) and modifies them for game compatibility
- **AssetRegistry integration** — Creates the necessary AssetRegistry.bin file so Fortnite recognizes your custom skin
- **Official engine support** — Works with vanilla Unreal Engine 4.26(.2) (no need to download or compile the modded version)
- **Fast workflow** — Backport a custom skin in under 1 minute

## Requirements

Before using this tool, you need to install and configure:

- **Windows OS** (tested on Windows 10/11)
- **.NET Framework** (version required by your system)
- **Blender 4.5** — [Download here](https://www.blender.org/download/)
  - **PSA/PSK Importer Plugin** — Required. [Download here](https://extensions.blender.org/download/sha256:9301a57466e3d41907b4b3175a7cc0e5df80aaefcc594676e5b898fdf13e7ad2/add-on-io-scene-psk-psa-v8.2.4.zip?repository=%2Fapi%2Fv1%2Fextensions%2F&blender_version_min=4.4.0&blender_version_max=5.0.0), then install: Edit → Preferences → Add-ons → Install from File, select the plugin, and enable it
- **Unreal Engine 4.26** — [Download from Epic Games Launcher](https://www.epicgames.com/store/en-US/download). Open Unreal Engine, create a new project and then go to Edit->Plugins and Enable
  - `Python Editor Script Plugin` plugin (built-in)
  - Enable `Editor Scripting Utilities` plugin (built-in)

## Installation

### Option 1: Use the Compiled .EXE (Recommended)
1. Download the latest `.exe` from [Releases](https://github.com/notzyron/Ultimate-Fortnite-Modding-Tool/releases)
2. Run the `.exe`
3. **Configure settings** (first launch):
   - Go to Settings page
   - Set **Blender executable path** (e.g., `C:\Program Files\Blender Foundation\Blender 4.5\blender.exe`)
   - Set **UE executable path** (e.g., `C:\Program Files\Epic Games\UE_4.26\Engine\Binaries\Win64\UE4Editor.exe`)
   - Set **UE project path** (path to your Fortnite UE 4.26 project)
   - Select **UE version** (Original UE 4.26 or modded version for Fortnite modding)
   - Save settings

### Option 2: Build from Source
1. Clone or download this repository
2. Open `UFMT.sln` in Visual Studio
3. Build the solution (Build → Build Solution)
4. Run the application
5. Follow the settings configuration steps above

## Usage

### Setup

1. Click **"Create Skin Folder"** and enter your skin's codename (e.g., `QuarterClaspZoom` from the Character ID)
2. Place your exported meshes in `[skin codename]/Source/` (name them descriptively, e.g., `Body.psk`)
3. Place textures and icons in `[skin codename]/Source/Textures/`

### Configure & Preview

1. Specify your skin folder path in **"Current Skin Path"**
2. The program auto-detects materials and assigns textures (may have issues with reskins)
3. Manually adjust texture assignments if needed using the dropdowns
4. Enter skin details: **name**, **description**, **rarity**, **gender**
5. Click **"Render"** to preview the skin
6. If the skin looks too shiny, enable **"Swizzle Roughness to Green"** and render again

### Export & Deploy

1. Click **"Export"**
2. The program will:
   - Convert .psk files to .fbx in Blender
   - Import meshes and textures into Unreal Engine
   - Apply correct settings to the meshes and textures and cook assets
   - Generate all required cosmetic files
   - Create AssetRegistry.bin for game recognition
3. Your finished skin will be in `[skin codename]/Output/FortniteGame/`
4. Pak the output folder with u4pak, move to your Fortnite v14.30 build, and launch the game!

**Note:** The export process may take several minutes as Blender and UE scripts run in the background.

## Compatibility

- **Fortnite Versions:** v14.30
- **Unreal Engine:** 4.26 (original or modded for Fortnite)
- **Blender:** 4.5

## Credits

- **UAssetAPI** — Asset handling and parsing library
- **Win3** — UI framework
- **QueenIO** — Asset extraction techniques (adapted code with attribution)
- **AssetRegistryInjector** — Registry injection utilities (adapted code with attribution)

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## Troubleshooting

- **Plugins not found in UE:** Ensure Python Script Editor and Scripting Utilities are enabled in Edit → Plugins
- **Blender scripts failing:** Verify Python is accessible from your Blender installation
- **Import stuck:** Check that all paths in Settings are correct and files exist

---

**Have questions or found a bug?** Open an issue on GitHub!
