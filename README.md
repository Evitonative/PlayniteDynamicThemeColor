# 🎨 Playnite Dynamic Theme Color
> [!CAUTION]
> **This is the development branch for Version 2 of this add-on.**
>
> It is currently work-in-progress, incomplete, and actively changing. Breaking changes and refactorings can happen at any time. Do not use this branch for production environments.

A [Playnite](https://playnite.link/) Add-on that dynamically changes your interface theme colors based on the cover or background artwork of the currently selected game.

## 🖼️ Showcase
See the add-on in action in [this video by u/shawak456 on Reddit](https://www.reddit.com/r/playnite/comments/1x32l0j/theres_finally_an_addon_for_dynamic_ui_color/).

> [!IMPORTANT]
> The add-on is designed and tested for Playnite's Default theme and [Harmony](https://github.com/darklinkpower/Harmony) in **Desktop Mode**.
> Other themes may not react to changes made by this add-on.
>
> Fullscreen Mode has no supported themes and you may want to disable the add-on when switching into fullscreen.
>
> ↳ *See [Theme Compatibility](#-theme-compatibility) below for details.*

> [!NOTE]
> I do not plan to officially support additional themes or Fullscreen Mode, but pull requests are welcome!
>
> This add-on aims to be non-destructive, so actual modifications to theme-files will never be included.

## ⭐ Features
* **Dynamic Theming:** Extracts color palettes from game artwork and applies them in real-time.
* **Non-Destructive:** All changes are memory-only and will not overwrite your installed theme.
* **Fast:** Colors are extracted using Google's Material Color Utilities once and cached.
* **Configuration:**
  * **Theme Styles:** Different theme styles from more muted colors to hue-shifted palettes.
  * **Accent Overrides:** Bypass blending and use the extracted color for UI accents.
  * **Image Priority:** Prioritizes Cover Art or Background Images for color extraction.
  * **Color Variety:** Deterministic variety slider for a wider range of dominant colors.
  * **View-Specific Rules:** Enable or disable dynamic colors per view.
  * **Custom Fallback Color:** Define a custom color when a game has no artwork, or when navigating disabled views.
  * **Saturation and Lightness Adjustments**: Tune the colors to your preference.

## ⬇️ Installation
1. Download the `.pext` file from the [Latest Release](https://github.com/Evitonative/PlayniteDynamicThemeColor/releases/latest) page.
2. Double-click the downloaded file.
3. Confirm the installation in Playnite and restart the application when prompted.

> [!WARNING]
> This Add-on is not currently available via Playnite's integrated Add-on browser because the Playnite team is temporarily pausing new add-on submissions. Manual installation is required.

## 🧭 Theme Compatibility
The Add-on has been tested with default and Harmony theme. Other themes may or may not work.

### Default Theme
Some buttons may have illegible hover text colors; this is inherent to how the default theme handles those colors and cannot be changed dynamically.

### Harmony
Harmony hardcodes the background color of the `Install` and `More` buttons, so they won't be changed dynamically.

<details>
 <summary>Manual Modification of Harmony to Recolour Context Buttons</summary>

 If you want those to change you have a couple options.

 You need to edit the `DerivedStyles\PlayButton.xaml` file, located by default at `%AppData%\Playnite\Themes\Desktop\Harmony_d49ef7bc-49de-4fd0-9a67-bd1f26b56047\DerivedStyles\PlayButton.xaml`.

 In there search for `#363742` or go to line 68, where you should find this:

 ```xaml
 <Style x:Key="ContextButtonViews" TargetType="{x:Type Button}">
     <Setter Property="Background" Value="#363742" />
     <Setter Property="Foreground" Value="{DynamicResource GlyphBrush}" />
     <Setter Property="BorderBrush" Value="{DynamicResource NormalBorderBrush}" />
 ```

 You can replace `#363742` with one of the following.
 - `{DynamicResource ControlBackgroundBrush}`: This is background used by the Edit and Favorite buttons.
 - `{DynamicResource SecondaryBrush}`: Is not used by the theme (or I couldn't find it). It for the most part also uses a fitting color.
</details>

## ⚖️ License & Copyright
**Dynamic Theme Color**
Copyright (C) 2026 Evito

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
[GNU General Public License](https://www.gnu.org/licenses/) for more details.
