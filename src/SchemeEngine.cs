using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using DynamicThemeColor.Model;
using DynamicThemeColor.UI.Views;
using Newtonsoft.Json;
using Playnite.SDK;

namespace DynamicThemeColor {
    public class SchemeEngine {
        private const string bundledSchemesFolderName = "assets/schemes";
        private const string themeSchemeFileName = "dtc-scheme.json";
        private const string schemesFolderName = "schemes";

        private static readonly Guid defaultSchemeId = Guid.Parse("acf38ef2-09da-4e3e-a24a-8cd8719fb2bc");
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI playniteApi;
        private readonly Guid addonId;
        private readonly Func<AddonSettings> addonSettings;
        private readonly Action<AddonSettings> savePluginSettings;

        public readonly Dictionary<Guid, RegisteredColorScheme> RegisteredSchemes = new Dictionary<Guid, RegisteredColorScheme>();
        public readonly Dictionary<string, ColorScheme.IColorReference> ActiveVariables = new Dictionary<string, ColorScheme.IColorReference>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, ColorScheme.IBrushDefinition> ActiveBrushes = new Dictionary<string, ColorScheme.IBrushDefinition>(StringComparer.OrdinalIgnoreCase);
        public List<Guid> ActiveSchemeChain { get; private set; } = new List<Guid>();

        public SchemeEngine(
            IPlayniteAPI playniteApi,
            Guid addonId,
            Func<AddonSettings> addonSettings,
            Action<AddonSettings> savePluginSettings) {
            this.playniteApi = playniteApi;
            this.addonId = addonId;
            this.addonSettings = addonSettings;
            this.savePluginSettings = savePluginSettings;

            PrepareSchemes();
            RefreshBrushes();
        }

        public void PrepareSchemes() {
            DiscoverSchemeFiles(out List<SchemeFile> schemeFiles);
            LoadSchemes(schemeFiles, out List<LoadingError> loadingErrors);

            foreach (LoadingError err in loadingErrors) {
                logger.Warn(err.ToString());
            }
        }

        private void DiscoverSchemeFiles(out List<SchemeFile> schemeFiles) {
            schemeFiles =  new List<SchemeFile>();

            string playniteAddonDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string playniteConfigPath = playniteApi.Paths.ConfigurationPath;
            string playniteExtensionsDataPath = playniteApi.Paths.ExtensionsDataPath;

            // Shipped with add-on
            if (playniteAddonDirectoryPath != null) {
                string addonDirectorySchemesPath = Path.Combine(playniteAddonDirectoryPath, bundledSchemesFolderName);
                if (!Directory.Exists(addonDirectorySchemesPath)) logger.Warn($"Add-on is missing built-in schemes");
                else schemeFiles.AddRange(Directory
                    .GetFiles(addonDirectorySchemesPath, "*.json", SearchOption.TopDirectoryOnly)
                    .Select(schemeFilePath => new SchemeFile(schemeFilePath, SchemeSource.BuiltIn)));
            } else logger.Error("Add-on assembly directory was not found.");

            // In Themes
            string themesPath = Path.Combine(playniteConfigPath, "Themes");
            string desktopThemesPath = Path.Combine(themesPath, "Desktop");
            string fullscreenThemePaths = Path.Combine(themesPath, "Fullscreen");

            if (!Directory.Exists(desktopThemesPath)) logger.Warn($"Desktop themes directory not found: {desktopThemesPath}");
            else schemeFiles.AddRange(
                from dir in Directory.GetDirectories(desktopThemesPath, "*", SearchOption.TopDirectoryOnly)
                select Path.Combine(dir, themeSchemeFileName)
                into schemeFilePath
                where File.Exists(schemeFilePath)
                select new SchemeFile(schemeFilePath, SchemeSource.BuiltIn));

            if (!Directory.Exists(fullscreenThemePaths)) logger.Warn($"Fullscreen themes directory not found: {fullscreenThemePaths}");
            else schemeFiles.AddRange(
                from dir in Directory.GetDirectories(fullscreenThemePaths, "*", SearchOption.TopDirectoryOnly)
                select Path.Combine(dir, themeSchemeFileName)
                into schemeFilePath
                where File.Exists(schemeFilePath)
                select new SchemeFile(schemeFilePath, SchemeSource.BuiltIn));

            // In Extension Setting
            string extensionSettingsPath = Path.Combine(playniteExtensionsDataPath, addonId.ToString());
            string settingsSchemesPath = Path.Combine(extensionSettingsPath, schemesFolderName);

            if (!Directory.Exists(settingsSchemesPath)) {
                logger.Debug($"Creating schemes folder at {settingsSchemesPath}");
                Directory.CreateDirectory(settingsSchemesPath);
            }

            schemeFiles.AddRange(Directory
                .GetFiles(settingsSchemesPath, "*.json", SearchOption.TopDirectoryOnly)
                .Select(schemeFilePath => new SchemeFile(schemeFilePath, SchemeSource.Custom)));
        }

        private void LoadSchemes(
            List<SchemeFile> files,
            out List<LoadingError> errors) {
            errors = new List<LoadingError>();
            RegisteredSchemes.Clear();

            foreach (SchemeFile file in files) {
                try {
                    string json = File.ReadAllText(file.Path);

                    ColorScheme scheme = JsonConvert.DeserializeObject<ColorScheme>(json);

                    if (scheme == null) {
                        errors.Add(new LoadingError("File content evaluated to null", file));
                        continue;
                    }

                    if (!scheme.Validate(out List<string> validationErrors)) {
                        errors.AddRange(validationErrors.Select(e => new LoadingError(e, file)));
                        continue;
                    }

                    RegisteredColorScheme registeredScheme = new RegisteredColorScheme(scheme, file.Path, file.Source);
                    RegisteredSchemes[registeredScheme.Scheme.Id] = registeredScheme;
                }
                catch (JsonException jsonEx) {
                    errors.Add(new LoadingError($"JSON parsing error: {jsonEx.Message}", file));
                }
                catch (Exception ex) {
                    errors.Add(new LoadingError($"Unexpected error reading file: {ex.Message}", file));
                }
            }
        }

        public void RefreshBrushes() {
            string currentDesktopTheme = playniteApi.ApplicationSettings.DesktopTheme;
            string currentFullscreenTheme = playniteApi.ApplicationSettings.FullscreenTheme;
            bool fullscreenEnabled = playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
            string currentTheme = fullscreenEnabled ? currentFullscreenTheme : currentDesktopTheme;

            logger.Trace($"Refreshing brushes. Mode: {(fullscreenEnabled ? "Fullscreen" : "Desktop")}, Active Theme: '{currentTheme}'");

            Guid bestMatchingScheme = TryDiscoverBestScheme(currentTheme, fullscreenEnabled);
            logger.Debug($"Best matching scheme resolved to: '{bestMatchingScheme}'");

            List<Guid> chain = new List<Guid>();
            UpdateVariableAndBrushDict(bestMatchingScheme, seenThemes: chain);
            ActiveSchemeChain = chain;
            logger.Trace($"Active scheme inheritance chain built: [{string.Join(" -> ", chain)}]");
        }

        private Guid TryDiscoverBestScheme(string currentTheme, bool fullscreenEnabled) {
            logger.Trace($"Searching for best scheme matching theme '{currentTheme}' (Fullscreen: {fullscreenEnabled}) among {RegisteredSchemes.Count} registered schemes.");

            List<RegisteredColorScheme> candidates = RegisteredSchemes.Values
                .Where(s => s.Scheme.Fullscreen == fullscreenEnabled)
                .Where(s => s.Scheme.Themes.Any(supportedTheme => currentTheme.IndexOf(supportedTheme, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (candidates.Count > 1) {
                logger.Debug($"Found {candidates.Count} matching scheme candidates, prompting user.");
                if (PromptUserToPickScheme(candidates, currentTheme, out RegisteredColorScheme userPickedScheme))
                    return userPickedScheme.Scheme.Id;
            }

            if (candidates.Count == 1) {
                RegisteredColorScheme candidate = candidates[0];
                logger.Trace($"Found matching scheme candidate: {candidate}");
                return candidate.Scheme.Id;
            }

            logger.Debug($"No scheme matched active theme '{currentTheme}'. Falling back to default.");
            return defaultSchemeId;
        }

        private Guid? TryDiscoverSchemeDependency(Guid dependencyGuid) {
            logger.Trace($"Searching for scheme dependency '{dependencyGuid}' among {RegisteredSchemes.Count} registered schemes.");

            List<RegisteredColorScheme> candidates = RegisteredSchemes.Values
                .Where(s => s.Scheme.Id == dependencyGuid)
                .ToList();

            if (candidates.Count > 1) {
                logger.Debug($"Found {candidates.Count} matching scheme candidates, prompting user.");
            }

            if (candidates.Count >= 1) {
                RegisteredColorScheme candidate = candidates[0];
                logger.Trace($"Found matching scheme candidate: {candidate}");
                return candidate.Scheme.Id;
            }

            if (dependencyGuid != defaultSchemeId) {
                logger.Warn($"Scheme dependency '{dependencyGuid}' could not be found. Falling back to 'default'.");
                return defaultSchemeId;
            }

            logger.Error("Default scheme could not be found.");
            return null;
        }

        private void UpdateVariableAndBrushDict(Guid schemeId, List<Guid> seenThemes = null, bool first = true) {
            if (seenThemes == null) seenThemes = new List<Guid>();

            if (first) {
                ActiveVariables.Clear();
                ActiveBrushes.Clear();
            }

            if (seenThemes.Contains(schemeId)) {
                logger.Error($"Circular inheritance detected with scheme '{schemeId}'");
                // TODO: Notify user
                return;
            }

            seenThemes.Add(schemeId);

            if (!RegisteredSchemes.TryGetValue(schemeId, out RegisteredColorScheme registeredScheme)) {
                // TODO: This should be the case when a scheme depends on a scheme that was not loaded. We should ask the user if they want the default scheme.
                logger.Error($"Invalid scheme in while flattening: {schemeId}");
                return;
            }

            ColorScheme scheme = registeredScheme.Scheme;

            if (scheme.Inherits != null) {
                Guid? bestMatchingScheme = TryDiscoverSchemeDependency(scheme.Inherits.Value);

                if (bestMatchingScheme == null)
                    logger.Warn($"Scheme dependency {scheme.Inherits} for {scheme.Themes} could not be found.");
                else UpdateVariableAndBrushDict(bestMatchingScheme.Value, seenThemes, first: false);
            }

            scheme.Variables.ForEach(kvp => ActiveVariables[kvp.Key] = kvp.Value);
            AddonSettings settings = addonSettings();
            foreach (KeyValuePair<string, ColorScheme.IBrushDefinition> kvp in scheme.Brushes) {
                string brushKey = kvp.Key;
                ColorScheme.IBrushDefinition brushDefinition = kvp.Value;

                if (brushDefinition.Optional != null) {
                    bool isEnabled = brushDefinition.Optional.Default;
                    if (settings?.SchemeOptionOverrides != null &&
                        settings.SchemeOptionOverrides.TryGetValue(scheme.Id, out Dictionary<string, bool> brushDict) &&
                        brushDict.TryGetValue(brushKey, out bool savedValue)) isEnabled = savedValue;

                    logger.Trace($"Optional brush '{brushKey}' in scheme '{scheme.Themes}' evaluated to enabled: {isEnabled}");
                    if (!isEnabled) continue;
                }

                ActiveBrushes[kvp.Key] = kvp.Value;
            }
        }

        private bool PromptUserToPickScheme(IReadOnlyCollection<RegisteredColorScheme> candidates, string theme, out RegisteredColorScheme scheme) {
            if (addonSettings().RememberedSchemeForThemes.TryGetValue(theme, out Guid schemeId)) {
                RegisteredColorScheme foundCandidate = candidates.FirstOrDefault(s => s.Scheme.Id == schemeId);
                if (foundCandidate != null) {
                    scheme = foundCandidate;
                    return true;
                }

                addonSettings().RememberedSchemeForThemes.Remove(theme);
                savePluginSettings(addonSettings());
            }

            Window window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
            });

            window.Title = ResourceProvider.GetString("LOCDTCSchemeSelectWindowTitel");
            window.Width = 550;
            window.SizeToContent = SizeToContent.Height;
            window.MinHeight = 200;
            window.MaxHeight = 450;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            SchemeSelectorViewModel viewModel = new SchemeSelectorViewModel(candidates, theme);
            SchemeSelectorView selectorView = new SchemeSelectorView { DataContext = viewModel };
            window.Content = selectorView;

            bool? result = window.ShowDialog();
            if (result != true || viewModel.SelectedItem == null) {
                scheme = null;
                return false;
            }

            scheme = viewModel.SelectedItem;
            if (!viewModel.RememberChoice) return true;

            addonSettings().RememberedSchemeForThemes[theme] = scheme.Scheme.Id;
            savePluginSettings(addonSettings());
            return true;
        }

        private readonly struct SchemeFile {
            public string Path { get; }
            public SchemeSource Source { get; }

            public SchemeFile(string path, SchemeSource source) {
                Path = path;
                Source = source;
            }

            public override string ToString() {
                return $"{Path} ({Source})";
            }
        }

        private readonly struct LoadingError {
            private string Message { get; }
            private SchemeFile File { get; }

            public LoadingError(string message, SchemeFile file) {
                Message = message;
                File = file;
            }

            public override string ToString() {
                return $"Error loading scheme file: {File}: {Message}";
            }
        }
    }
}
