using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;

namespace DynamicThemeColor.Theming {
    public class ThemeRegistry {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly Dictionary<string, ThemeDefinition> loadedThemes = new Dictionary<string, ThemeDefinition>(StringComparer.OrdinalIgnoreCase);

        public void LoadThemes() {
            ThemeDefinition defaultTheme = new ThemeDefinition {
                Name = "Default",
                Variables = {
                    // Accent
                    { "AccentColor", "Primary80" },
                    { "OnAccentColor", "Primary10" },

                    // Surfaces & Window Backdrops
                    { "WindowBackground", "Neutral8" },
                    { "TooltipBackground", "NeutralVariant8" },
                    { "WindowOutline", "NeutralVariant35" },

                    // Typography
                    { "PrimaryText", "Neutral90" },
                    { "SecondaryText", "NeutralVariant70" },

                    // Buttons & Inputs
                    { "ButtonBackground", "Neutral25" },
                    { "ButtonOutline", "Primary70" },

                    // Other Colors
                    { "ScrollbarInactiveThumb", "Neutral20" },
                    { "ProgressbarBackground", "NeutralVariant15" },
                },
                Brushes = {
                    // --- Surfaces & Window Backdrops ---
                    // ReSharper disable once StringLiteralTypo - Playnite dev made a typo
                    { "WindowBackgourndBrush", "@WindowBackground" }, // Primary window backgrounds
                    { "WindowBackgroundBrush", "@WindowBackground" }, // Primary window backgrounds
                    { "PopupBackgroundBrush", "@TooltipBackground" }, // Popup Background
                    { "TooltipBackgroundBrush", "@TooltipBackground" }, // Tooltip Background
                    { "PopupBorderBrush", "@WindowOutline" }, // Window Outline
                    { "GridItemBackgroundBrush", "@TooltipBackground" }, // Background under letterboxed/odd-aspect covers in Grid View

                    // --- Typography ---
                    { "TextBrush", "@PrimaryText" }, // Primary text across labels, titles, and grid tiles
                    { "TextBrushDarker", "@SecondaryText" }, // The search icon, Harmony uses it in more places
                    { "TextBrushDark", "@OnAccentColor" }, // The Button on the play button

                    // --- Interactive Accents, Glyphs & State Indicators ---
                    { "GlyphBrush", "@AccentColor" }, // Used by a lot, play button, active elements, some Glyphs
                    { "HighlightGlyphBrush", "@AccentColor" }, // Background of the progress in sliders
                    { "CheckBoxCheckMarkBrush", "@AccentColor" }, // Checkbox check glyph
                    { "HoverBrush", "@AccentColor:80" }, // Hovering on sidebar

                    // --- Controls, Inputs & Buttons ---
                    { "ButtonBackgroundBrush", "@ButtonBackground" }, // Button Background
                    { "NormalBorderBrush", "@ButtonOutline:32" }, // Outline of buttons
                    { "ControlBackgroundBrush", "@ButtonBackground:64" }, // Sidebar, Searchbar, Search Menu, All kinds of inputs
                    { "NormalBrush", "@ScrollbarInactiveThumb" }, // Inactive Scrollbar & Filters Button
                    { "NormalBrushDark", "@ProgressbarBackground" }, // Progress bar gutter / recessed tracks

                    // --- Layout Framing & Dividers ---
                    { "PanelSeparatorBrush", "Transparent" }, // Framing in main window
                    { "WindowPanelSeparatorBrush", "Transparent" }, // Framing in the settings windows
                    { "ExpanderBackgroundBrush", "Transparent"  } // Expandable section backgrounds
                },
            };

            ThemeDefinition harmony = new ThemeDefinition {
                Name = "Harmony",
                InheritsFrom = new[]{ "Default" },
                Variables = {
                    { "CardBackground", "NeutralVariant15" },
                    { "AltPropertiesButton", "NeutralVariant30" },
                },
                Brushes = {
                    { "MainWindowBackgroundBrush", "@WindowBackground" }, // Main Window Background
                    { "PlayButtonTextBrush", "@OnAccentColor" }, // Text play button
                    { "ButtonMouseOverBackgroundBrush", "@ButtonBackground:128" }, // Hover over game quick actions
                    { "SecondaryBrush", "@AltPropertiesButton" }, // Unused but I use it as alternative disabled Properties Button
                    { "MainPanelBackgroundBrush", "@AccentColor" }, // Used by borders from checkboxes, but I can't see it

                    // TODO: Somehow add the optional thing from settings
                    { "DetailsViewPanelBackgroundBrush", "@CardBackground"},
                },
            };

            loadedThemes[defaultTheme.Name] = defaultTheme;
            loadedThemes[harmony.Name] = harmony;
        }

        public void ExtractThemeDefinitions(
            string themeName,
            out Dictionary<string, string> unifiedVariables,
            out Dictionary<string, string> unifiedBrushes) {

            if (!TryFindTheme(themeName, out ThemeDefinition theme)) {
                logger.Warn($"Theme {themeName} not found, falling back to Default");

                if (!loadedThemes.TryGetValue("Default", out theme)) {
                    logger.Error("Default theme not found using empty theme.");
                    theme = new ThemeDefinition {
                        Name = "Default",
                    };
                }
            }

            unifiedVariables = new Dictionary<string, string>();
            unifiedBrushes = new Dictionary<string, string>();
            HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ResolveThemeVariables(theme, unifiedVariables, unifiedBrushes, visiting, visited);
        }

        public Dictionary<string, (uint color, byte alpha)> ResolveDictionary(
            Dictionary<string, string> entriesToResolve,
            ColorPalette colorPalette,
            Dictionary<string, (uint color, byte alpha)> resolvedVariables = null,
            Dictionary<string, string> rawVariables = null) {

            Dictionary<string, (uint color, byte alpha)> results = new Dictionary<string, (uint color, byte alpha)>();
            if (resolvedVariables == null) resolvedVariables = results;

            HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> entry in entriesToResolve) {
                if (results.ContainsKey(entry.Key)) continue;

                logger.Trace($"Resolving '{entry.Key}' -> '{entry.Value}'");

                visiting.Clear();
                if (ReferenceEquals(results, resolvedVariables)) {
                    visiting.Add(entry.Key);
                }

                (uint color, byte alpha)? resolved = ResolveBrush(
                    entry.Value,
                    resolvedVariables,
                    rawVariables,
                    colorPalette,
                    visiting
                );

                if (resolved != null) {
                    results[entry.Key] = resolved.Value;
                }

            }

            return results;
        }

        private bool TryFindTheme(string playniteThemeId, out ThemeDefinition theme) {
            if (string.IsNullOrWhiteSpace(playniteThemeId)) {
                theme = null;
                return false;
            }

            if (loadedThemes.TryGetValue(playniteThemeId, out theme)) {
                return true;
            }

            foreach (KeyValuePair<string, ThemeDefinition> kvp in loadedThemes.Where(kvp => playniteThemeId.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                theme = kvp.Value;
                return true;
            }

            theme = null;
            return false;
        }

        private void ResolveThemeVariables(
            ThemeDefinition currentTheme,
            Dictionary<string, string> unifiedVariables,
            Dictionary<string, string> unifiedBrushes,
            HashSet<string> visiting,
            HashSet<string> visited) {

            if (currentTheme == null) return;

            if (visiting.Contains(currentTheme.Name)) {
                logger.Warn($"Circular dependency detected involving theme '{currentTheme.Name}'. Skipping.");
                return;
            }

            if (visited.Contains(currentTheme.Name)) {
                return;
            }

            visiting.Add(currentTheme.Name);

            string[] dependencies = currentTheme.InheritsFrom ?? Array.Empty<string>();
            if (dependencies.Length > 0) {
                int validDependenciesFound = 0;

                foreach (string dependencyName in dependencies) {
                    if (loadedThemes.TryGetValue(dependencyName, out ThemeDefinition dependencyTheme)) {
                        validDependenciesFound++;
                        ResolveThemeVariables(dependencyTheme, unifiedVariables, unifiedBrushes, visiting, visited);
                    } else {
                        logger.Warn($"Dependency '{dependencyName}' for theme '{currentTheme.Name}' not found. Skipping.");
                    }
                }

                if (validDependenciesFound == 0 && currentTheme.InheritsFrom.Length > 0) {
                    logger.Warn($"All dependencies for '{currentTheme.Name}' were missing. Falling back to 'Default'.");

                    if (loadedThemes.TryGetValue("Default", out ThemeDefinition defaultTheme)) {
                        ResolveThemeVariables(defaultTheme, unifiedVariables, unifiedBrushes, visiting, visited);
                    } else {
                        logger.Warn("Fallback 'Default' theme is also missing. Skipping.");
                    }
                }
            }

            if (currentTheme.Variables != null) {
                foreach (KeyValuePair<string, string> variable in currentTheme.Variables) {
                    unifiedVariables[variable.Key] = variable.Value;
                }
            }

            if (currentTheme.Brushes != null) {
                foreach (KeyValuePair<string, string> brush in currentTheme.Brushes) {
                    unifiedBrushes[brush.Key] = brush.Value;
                }
            }

            visiting.Remove(currentTheme.Name);
            visited.Add(currentTheme.Name);
        }

        private static (uint, byte)? ResolveBrush(
            string input,
            Dictionary<string, (uint color, byte alpha)> resolvedVariables,
            Dictionary<string, string> rawVariables,
            ColorPalette colorPalette,
            HashSet<string> visiting) {

            string[] alphaParts = input.Split(new[] { ':' }, 2);
            string brush = alphaParts[0];
            string alphaStr = alphaParts.Length > 1 ? alphaParts[1] : string.Empty;

            byte? alpha = null;
            if (!string.IsNullOrWhiteSpace(alphaStr)) {
                if(!byte.TryParse(alphaStr, out byte parseAlpha)) return null;
                alpha = parseAlpha;
            }

            uint color;
            switch (brush) {
                case "Transparent":
                    logger.Debug($"Brush '{brush}' is transparent.");
                    return (0, 0);

                case string br when br.StartsWith("#"):
                    uint? hexColor = ColorUtilities.GetColorFromHex(br);
                    if (hexColor == null) {
                        logger.Warn($"Brush '{brush}' has an invalid hex color ({br}).");
                        return null;
                    }
                    color = hexColor.Value;
                    break;

                case string br when br.StartsWith("@"):
                    string variableName = br.Substring(1);

                    if (!resolvedVariables.TryGetValue(variableName, out (uint color, byte alpha) resolved)) {
                        if (rawVariables == null || !rawVariables.TryGetValue(variableName, out string rawValue)) {
                            logger.Warn($"Brush '{brush}' has an invalid variable name ({variableName}).");
                            return null;
                        }

                        if (!visiting.Add(variableName)) {
                            logger.Warn($"Circular variable reference detected involving '@{variableName}'.");
                            return null;
                        }

                        (uint color, byte alpha)? nested = ResolveBrush(rawValue, resolvedVariables, rawVariables, colorPalette, visiting);
                        visiting.Remove(variableName);

                        if (nested == null) return null;

                        resolved = nested.Value;
                        resolvedVariables[variableName] = resolved;
                    }

                    color = resolved.color;
                    if (alpha == null) alpha = resolved.alpha;
                    break;

                default:
                    string[] materialParts = Regex.Split(input, @"(?<=\D)(?=\d)");

                    if (materialParts.Length != 2) {
                        logger.Warn($"Unable to parse brush {brush}");
                        return null;
                    }

                    string paletteName = materialParts[0];
                    string valueStr = materialParts[1];

                    if (uint.TryParse(valueStr, out uint materialValue)) {
                        if (materialValue > 100) {
                            logger.Warn($"Brush '{brush}' has an cannot be greater than 100 ({valueStr}). Using 100.");
                            materialValue = 100;
                        }
                    } else {
                        logger.Warn($"Unable to parse brush {brush}");
                        return null;
                    }

                    switch (paletteName) {
                        case "Neutral":
                            color = colorPalette.Palette.Neutral[materialValue];
                            break;
                        case "NeutralVariant":
                            color = colorPalette.Palette.NeutralVariant[materialValue];
                            break;
                        case "Error":
                            color = colorPalette.Palette.Error[materialValue];
                            break;
                        case "Primary":
                            color = colorPalette.Palette.Primary[materialValue];
                            break;
                        case "Secondary":
                            color = colorPalette.Palette.Secondary[materialValue];
                            break;
                        case "Tertiary":
                            color = colorPalette.Palette.Tertiary[materialValue];
                            break;
                        default:
                            logger.Warn($"Brush '{brush}' has an invalid palette name ({paletteName}).");
                            return null;
                    }

                    break;
            }

            if (alpha == null) alpha = 0xff;
            return (color, (byte)alpha);
        }
    }
}
