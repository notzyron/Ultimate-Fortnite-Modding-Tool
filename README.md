# Ultimate Fortnite Modding Tool

A modding tool for importing the latest Fortnite assets to older Fortnite builds.

## Features

- **Automated mesh conversion** — Converts exported .psk mesh files to .fbx format automatically using Blender
- **Intelligent material setup** — Automatically pairs textures (diffuse, normal, mask, specular) with their corresponding materials, with manual override options
- **Real-time preview rendering** — Render and preview your skin in Blender's viewport to verify appearance before export (with roughness adjustment option for proper material rendering)
- **One-click asset generation** — Automatically generates all required Fortnite cosmetic assets (meshes, materials, textures, CID, HID, HS, CP) and modifies them for game compatibility
- **AssetRegistry integration** — Creates the necessary AssetRegistry.bin file so Fortnite recognizes your custom skin
- **Official engine support** — Works with vanilla Unreal Engine 4.26 (no need to download or compile the massive modded version)
- **Fast workflow** — Complete custom skin import in under 1 minute (vs 10-15 minutes manually)

## Requirements

Before using this tool, you need to install and configure:

- **Windows OS** (tested on Windows 10/11)
- **.NET Framework** (version required by your system)
- **Blender 4.5** — [Download here](https://www.blender.org/download/)
  - **PSA/PSK Importer Plugin** — Available from a modding Discord community
  - **Better FBX Exporter Plugin** — Available from a modding Discord community
- **Unreal Engine 4.26** — [Download from Epic Games Launcher](https://www.epicgames.com/store/en-US/download)
  - Enable `Python Script Editor` plugin (built-in)
  - Enable `Scripting Utilities` plugin (built-in)
- **Python** — Required for Blender and UE scripting (comes with Blender, optional for UE)

## Installation

1. **Clone or download this repository**
2. **Build the project** in Visual Studio:
   - Open `UFMT.sln`
   - Build the solution (Build → Build Solution)
3. **Run the application**
4. **Configure settings** (first launch):
   - Go to Settings page
   - Set **Blender executable path** (e.g., `C:\Program Files\Blender Foundation\Blender 4.5\blender.exe`)
   - Set **UE executable path** (e.g., `C:\Program Files\Epic Games\UE_4.26\Engine\Binaries\Win64\UE4Editor.exe`)
   - Set **UE project path** (path to your Fortnite UE 4.26 project)
   - Select **UE version** (Original UE 4.26 or modded version for Fortnite modding)
   - Save settings

## Usage

### Setup

1. Click **"Create Skin Folder"** and enter your skin's codename (e.g., `QuarterClaspZoom` from the Character ID)
2. Place your exported meshes in `[skin codename]/Source/` (name them descriptively, e.g., `Body.psk`)
3. Place textures and icons in `[skin codename]/Source/Textures/`

### Configure & Preview

1. Specify your skin folder path in **"Current Skin Path"**
2. The program auto-detects materials and assigns textures
3. Manually adjust texture assignments if needed using the dropdowns
4. Enter skin details: **name**, **description**, **rarity**, **gender**
5. Click **"Render"** to preview the skin in Blender
6. If the skin looks too shiny, enable **"Swizzle Roughness to Green"** and render again

### Export & Deploy

1. Click **"Export"** to start the automated pipeline
2. The program will:
   - Convert .psk files to .fbx in Blender
   - Import meshes and textures into Unreal Engine
   - Apply correct material settings and cook assets
   - Generate all required cosmetic files
   - Create AssetRegistry.bin for game recognition
3. Your finished skin will be in `[skin codename]/Output/FortniteGame/`
4. Pak the output folder with u4pak, move to your Fortnite v14.30 build, and launch the game!

**Note:** The export process may take several minutes as Blender and UE scripts run in the background.

## Compatibility

- **Fortnite Versions:** v13.40 - v14.30
- **Unreal Engine:** 4.26 (original or modded for Fortnite)
- **Blender:** 4.5+

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
