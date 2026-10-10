# Dynamic Theme Color
A [Playnite](https://playnite.link/) Add-on that dynamically changes your interface theme colors based on the cover or background artwork of the currently selected game.

[Demo Video](https://github.com/user-attachments/assets/1a31d9a6-2b8c-4903-a4b4-1f580e853880)

This Add-on natively supports Playnite's Default theme and the [Harmony](https://github.com/darklinkpower/Harmony) theme. 
Other themes may or may not work depending on how they manage their colors.

> [!note]
> I do not plan to officially support additional themes, but pull requests are welcome!

## Features
* **Dynamic Theming:** Extracts color palettes from game artwork and applies them in real-time.
* **Non-Destructive:** All changes are memory-only and will not overwrite your installed theme.
* **Configuration:**
  * **Theme Styles:** Different theme styles from more muted colors to hue-shifted palletes.
  * **Accent Overrides:** Bypass blending and use the extracted color for UI accents.
  * **Image Priority:** Prioritizes Cover Art or Background Images for color extraction.
  * **Color Variety:** Deterministic variety slider for a wider range of dominant colors.
  * **View-Specific Rules:** Enable or disable dynamic colors per view.
  * **Custom Fallback Color:** Define a custom color when a game has no artwork, or when navigating disabled views.

## Installation
1. Download the `.pext` file from the [Latest Release](https://github.com/Evitonative/PlayniteDynamicThemeColor/releases/latest) page.
2. Double-click the downloaded file.
3. Confirm the installation in Playnite and restart the application when prompted.

> [!note]
> This Add-on is not currently available via Playnite's integrated Add-on browser because the Playnite team is temporarily pausing new add-on submissions. Manual installation is required.

## License & Copyright
**Dynamic Theme Color**
Copyright (C) 2026 Evito

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
[GNU General Public License](https://www.gnu.org/licenses/) for more details.
