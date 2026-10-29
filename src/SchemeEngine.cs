using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DynamicThemeColor.Model;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Playnite.SDK;

namespace DynamicThemeColor {
    public class SchemeEngine {
        private const string bundledSchemesFolderName = "assets/schemes";
        private const string themeSchemeFileName = "dtc-scheme.json";
        private const string schemesFolderName = "schemes";

        private readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI playniteApi;
        private readonly Guid addonId;
        private readonly Func<AddonSettings> addonSettings;

        public readonly Dictionary<string, ColorScheme> Schemes = new Dictionary<string, ColorScheme>(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string, int)> schemeFiles = new List<(string, int)>();
        public readonly Dictionary<string, ColorScheme.IColorReference> ActiveVariables = new Dictionary<string, ColorScheme.IColorReference>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ColorScheme.IBrushDefinition> ActiveBrushes = new Dictionary<string, ColorScheme.IBrushDefinition>(StringComparer.OrdinalIgnoreCase);

        public List<string> ActiveSchemeChain { get; private set; }

        public SchemeEngine(IPlayniteAPI playniteApi, Guid addonId, Func<AddonSettings> addonSettings) {
            this.playniteApi = playniteApi;
            this.addonId = addonId;
            this.addonSettings = addonSettings;

            PrepareSchemes();
            RefreshBrushes();
        }

        private void PrepareSchemes() {
            DiscoverSchemes();
            LoadSchemes(schemeFiles, out List<(string, string)> loadingErrors);

            foreach ((string file, string message) in loadingErrors)
            {
                logger.Warn($"Error loading scheme file: {file}: {message}");
            }
        }

        public void RefreshBrushes() {
            string currentDesktopTheme = playniteApi.ApplicationSettings.DesktopTheme;
            string currentFullscreenTheme = playniteApi.ApplicationSettings.FullscreenTheme;
            bool fullscreenEnabled = playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
            string currentTheme = fullscreenEnabled ? currentFullscreenTheme : currentDesktopTheme;

            logger.Trace($"Refreshing brushes. Mode: {(fullscreenEnabled ? "Fullscreen" : "Desktop")}, Active Theme: '{currentTheme}'");

            string bestMatchingScheme = TryDiscoverBestScheme(currentTheme, fullscreenEnabled);
            logger.Debug($"Best matching scheme resolved to: '{bestMatchingScheme}'");

            List<string> chain = new List<string>();
            MakeVariableAndBrushDict(bestMatchingScheme, ActiveVariables, ActiveBrushes, seenThemes: chain);
            ActiveSchemeChain = chain;
            logger.Trace($"Active scheme inheritance chain built: [{string.Join(" -> ", chain)}]");
        }

        private void DiscoverSchemes() {
            schemeFiles.Clear();

            string playniteAddonDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string playniteConfigPath = playniteApi.Paths.ConfigurationPath;
            string playniteExtensionsDataPath = playniteApi.Paths.ExtensionsDataPath;

            // Shipped with add-on
            if (playniteAddonDirectoryPath != null) {
                string addonDirectorySchemesPath = Path.Combine(playniteAddonDirectoryPath, bundledSchemesFolderName);
                if (!Directory.Exists(addonDirectorySchemesPath)) logger.Warn($"Add-on is missing built-in schemes");
                else foreach (string file in Directory.GetFiles(addonDirectorySchemesPath, "*.json", SearchOption.TopDirectoryOnly))
                    schemeFiles.Add((file, 10));
            } else logger.Error("Add-on assembly directory was not found.");

            // In Themes
            string themesPath = Path.Combine(playniteConfigPath, "Themes");
            string desktopThemesPath = Path.Combine(themesPath, "Desktop");
            string fullscreenThemePaths = Path.Combine(themesPath, "Fullscreen");

            if (!Directory.Exists(desktopThemesPath)) logger.Warn($"Desktop themes directory not found: {desktopThemesPath}");
            else foreach (string dir in Directory.GetDirectories(desktopThemesPath, "*", SearchOption.TopDirectoryOnly)) {
                string schemeFile = Path.Combine(dir, themeSchemeFileName);
                if (File.Exists(schemeFile)) schemeFiles.Add((schemeFile, 20));
            }

            if (!Directory.Exists(fullscreenThemePaths)) logger.Warn($"Fullscreen themes directory not found: {fullscreenThemePaths}");
            else foreach (string dir in Directory.GetDirectories(fullscreenThemePaths, "*", SearchOption.TopDirectoryOnly)) {
                string schemeFile = Path.Combine(dir, themeSchemeFileName);
                if (File.Exists(schemeFile)) schemeFiles.Add((schemeFile, 20));
            }

            // In Extension Setting
            string extensionSettingsPath = Path.Combine(playniteExtensionsDataPath, addonId.ToString());
            string settingsSchemesPath = Path.Combine(extensionSettingsPath, schemesFolderName);

            if (!Directory.Exists(settingsSchemesPath)) {
                logger.Debug($"Creating schemes folder at {settingsSchemesPath}");
                Directory.CreateDirectory(settingsSchemesPath);
            }

            foreach (string file in Directory.GetFiles(settingsSchemesPath, "*.json", SearchOption.TopDirectoryOnly))
                schemeFiles.Add((file, 30));
        }

        private void LoadSchemes(List<(string, int)> files, out List<(string, string)> errors) {
            errors = new List<(string, string)>();

            Schemes.Clear();

            foreach ((string filePath, int priority) in files) {
                try {
                    string json = File.ReadAllText(filePath);

                    ColorScheme scheme = JsonConvert.DeserializeObject<ColorScheme>(json);

                    if (scheme == null) {
                        errors.Add((filePath, $"File content evaluated to null"));
                        continue;
                    }

                    if (!scheme.Validate(out List<string> validationErrors)) {
                        errors.AddRange(validationErrors.Select(e => (filePath, e)));
                        continue;
                    }

                    RegisterOrUpdateScheme(scheme, priority);
                }
                catch (JsonException jsonEx) {
                    errors.Add((filePath, $"JSON parsing error: {jsonEx.Message}"));
                }
                catch (Exception ex) {
                    errors.Add((filePath, $"Unexpected error reading file: {ex.Message}"));
                }
            }
        }

        private void MakeVariableAndBrushDict(
            string schemeName,
            Dictionary<string, ColorScheme.IColorReference> variables,
            Dictionary<string, ColorScheme.IBrushDefinition> brushes,
            List<string> seenThemes = null,
            bool first = true) {

            if (seenThemes == null) seenThemes = new List<string>();

            if (first) {
                variables.Clear();
                brushes.Clear();
            }

            if (seenThemes.Contains(schemeName)) {
                logger.Error($"Circular inheritance detected with scheme '{schemeName}'");
                // TODO: Notify user
                return;
            }
            seenThemes.Add(schemeName);

            if (!Schemes.TryGetValue(schemeName, out ColorScheme scheme)) {
                logger.Error($"Invalid scheme in while flattening: {schemeName}");
                return;
            }

            if (scheme.Inherits != null) {
                bool fullscreenEnabled = playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;

                string bestMatchingScheme = TryDiscoverBestScheme(
                    scheme.Inherits.Theme,
                    fullscreenEnabled,
                    minVersion: scheme.Inherits.MinVersion,
                    authors: scheme.Inherits.Author);

                if (string.IsNullOrEmpty(bestMatchingScheme))
                    logger.Warn($"Scheme dependency {scheme.Inherits.Theme} for {scheme.Theme} could not be found.");
                else MakeVariableAndBrushDict(bestMatchingScheme, variables, brushes, seenThemes, first: false);
            }

            scheme.Variables.ForEach(kvp => variables[kvp.Key] = kvp.Value);
            AddonSettings settings = addonSettings();
            foreach (KeyValuePair<string, ColorScheme.IBrushDefinition> kvp in scheme.Brushes) {
                string brushKey = kvp.Key;
                ColorScheme.IBrushDefinition brushDefinition = kvp.Value;

                if (brushDefinition.Optional != null) {
                    bool isEnabled = brushDefinition.Optional.Default;
                    if (settings?.SchemeOptionOverrides != null &&
                        settings.SchemeOptionOverrides.TryGetValue(scheme.Theme, out Dictionary<string, bool> brushDict) &&
                        brushDict.TryGetValue(brushKey, out bool savedValue)) isEnabled = savedValue;

                    logger.Trace($"Optional brush '{brushKey}' in scheme '{scheme.Theme}' evaluated to enabled: {isEnabled}");
                    if (!isEnabled) continue;
                }

                brushes[kvp.Key] = kvp.Value;
            }
        }

        private string TryDiscoverBestScheme(string currentTheme, bool fullscreenEnabled) {
            logger.Trace($"Searching for best scheme matching theme '{currentTheme}' (Fullscreen: {fullscreenEnabled}) among {Schemes.Count} registered schemes.");

            List<ColorScheme> candidates = Schemes.Values
                .Where(s => s.Fullscreen == fullscreenEnabled)
                .Where(s => currentTheme.IndexOf(s.Theme, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(s => s.DiscoveryPriority)
                .ThenByDescending(s => s.Theme.Length)
                .ToList();

            if (candidates.Count > 0) {
                logger.Trace($"Found matching scheme candidate: '{candidates[0].Theme}' (Priority: {candidates[0].DiscoveryPriority})");
                return candidates[0].Theme;
            }

            logger.Debug($"No scheme matched active theme '{currentTheme}'. Falling back to default.");
            return "default";
        }

        [CanBeNull]
        private string TryDiscoverBestScheme(string dependencyName, bool fullscreenEnabled, int? minVersion, [CanBeNull] List<string> authors) {
            logger.Trace($"Searching for scheme dependency '{dependencyName}' (Fullscreen: {fullscreenEnabled}) among {Schemes.Count} registered schemes.");

            List<ColorScheme> candidates = Schemes.Values
                .Where(s => s.Fullscreen == fullscreenEnabled)
                .Where(s => string.Equals(s.Theme, dependencyName, StringComparison.OrdinalIgnoreCase))
                .Where(s=> minVersion == null || s.Version >= minVersion)
                .Where(s=> authors == null || new HashSet<string>(authors).SetEquals(s.Author))
                .OrderByDescending(s => s.DiscoveryPriority)
                .ThenByDescending(s => s.Theme.Length)
                .ToList();


            if (candidates.Count > 0) {
                logger.Trace($"Found matching scheme candidate: '{candidates[0].Theme}' (Priority: {candidates[0].DiscoveryPriority})");
                return candidates[0].Theme;
            }

            if (!string.Equals(dependencyName, "default", StringComparison.OrdinalIgnoreCase)) {
                logger.Warn($"Scheme dependency '{dependencyName}' could not be found. Falling back to 'default'.");
                return "default";
            }

            logger.Error("Default scheme could not be found.");
            return null;
        }

        private void RegisterOrUpdateScheme(ColorScheme scheme, int priority) {
            if (Schemes.TryGetValue(scheme.Theme, out ColorScheme existing))
                if (priority < existing.DiscoveryPriority) return;

            scheme.DiscoveryPriority = priority;
            Schemes[scheme.Theme] = scheme;
        }
    }
}
