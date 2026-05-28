# Ultimate Fortnite Modding Tool

A modding tool for importing the latest Fortnite assets to older Fortnite builds.

## Features

- **Automatic texture assignment** — Connects exported textures with the correct materials (may have issues with reskins)
- **Customize asset properties** — Configure metadata like name, description, rarity, and gender for each asset
- **Preview rendering** — Render a preview in Blender to see how your asset will look in-game (note: Blender and Unreal Engine rendering differs)
- **Convert PSK to FBX** — Converts exported .psk files from FModel to .fbx format in Blender
- **Automated asset export** — Imports converted models into Unreal Engine, applies settings, cooks assets, generates required cosmetic identifiers (CID, HID, HS, CP), and finalizes everything using UAssetAPI for in-game compatibility

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

1. **Open the application**
2. **Go to Skins page**
3. **Select the skin(s)** you want to import
4. **Click Import**
5. The tool will automatically:
   - Extract assets using UAssetAPI
   - Process materials and meshes in Blender
   - Import into your Unreal Engine project
   - Generate necessary files

**Note:** The first import may take several minutes as Blender and UE scripts run in the background.

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
