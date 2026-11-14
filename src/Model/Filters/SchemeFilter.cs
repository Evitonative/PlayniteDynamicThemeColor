using System;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Model.Filters {
    public abstract class SchemeFilter {
        /// <summary>
        /// The name of the filter
        ///
        /// This will be shown to the user.
        /// </summary>
        [CanBeNull]
        public virtual string Name => null;

        [CanBeNull]
        [JsonIgnore]
        public virtual FilterConfiguration FilterConfiguration { get; internal set; }

        [JsonIgnore]
        public FilterStage AppliedStage { get; internal set; }

        /// <summary>
        /// Prefilter Games before anything is done for a game.
        /// </summary>
        /// <param name="game">
        /// The current selected game.
        /// </param>
        /// <returns>true if a game should be excluded, false it shouldn't. Null for no action.</returns>
        public virtual bool? ApplyGameFilter(Game game) {
            return null;
        }

        /// <summary>
        /// Apply a color to the seed color for theming before palette generation.
        /// </summary>
        /// <param name="currentSeed">
        /// The current seed color in ARGB format.
        ///
        /// It may have been already modified by other filters beforehand.
        /// Other filters may be applied after.
        /// </param>
        /// <param name="colorExtractionResults">The results from the color extractor.</param>
        /// <param name="game">
        /// The current selected game.
        ///
        /// May be null if no game is selected.
        /// </param>
        /// <param name="playniteApi">The playnite API</param>
        /// <param name="addonSettings">The addons Settings</param>
        /// <returns>
        /// The new seed color in ARGB format.
        /// </returns>
        public virtual uint ApplySeedFilter(
            uint currentSeed, ColorExtractionResults colorExtractionResults,
            [CanBeNull] Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            return currentSeed;
        }

        /// <summary>
        /// Apply filters to color palette for theming before the brushes are created.
        /// </summary>
        /// <param name="palette">
        /// The color palette that will be used to create brushes.
        ///
        /// It may have been already modified by other filters beforehand.
        /// Other filters may be applied after.
        /// </param>
        /// <param name="colorExtractionResults">The results from the color extractor.</param>
        /// <param name="game">
        /// The current selected game.
        ///
        /// May be null if no game is selected.
        /// </param>
        /// <param name="playniteApi">The playnite API</param>
        /// <param name="addonSettings">The addons Settings</param>
        public virtual void ApplyPaletteFilter(
            ThemePalette palette, ColorExtractionResults colorExtractionResults,
            [CanBeNull] Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
        }

        /// <summary>
        /// Apply filters to the final color before a brush is used to paint the UI.
        /// </summary>
        /// <param name="brushKey">
        /// The name of the brush.
        /// </param>
        /// <param name="brushArgb">
        /// The current brush color in ARGB format.
        ///
        /// It may have been already modified by other filters beforehand.
        /// Other filters may be applied after.
        /// </param>
        /// <param name="colorExtractionResults">The results from the color extractor.</param>
        /// <param name="game">
        /// The current selected game.
        ///
        /// May be null if no game is selected.
        /// </param>
        /// <param name="playniteApi">The playnite API</param>
        /// <param name="addonSettings">The addons Settings</param>
        /// <returns>
        /// The new brush color in ARGB format.
        /// </returns>
        public virtual uint ApplyBrushFilter(
            string brushKey, uint brushArgb,
            [CanBeNull] ColorExtractionResults colorExtractionResults,
            [CanBeNull] Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            return brushArgb;
        }

        /// <summary>
        /// This is called when the addon is instructed to clear cache.
        /// </summary>
        public virtual void ClearCache() {
        }
    }

    public abstract class SchemeFilter<TSettings> : SchemeFilter where TSettings : class, new() {
        protected TSettings Settings {
            get => filterConfiguration.Settings as TSettings;
            private set {
                if (FilterConfiguration != null) FilterConfiguration.Settings = value;
            }
        }

        private FilterConfiguration filterConfiguration;
        public override FilterConfiguration FilterConfiguration {
            get => filterConfiguration;
            internal set {
                filterConfiguration = value;
                if (value == null) return;
                switch (value.Settings) {
                    case null:
                        Settings = new TSettings();
                        return;
                    case TSettings typed:
                        Settings = typed; return;
                    case JObject jObj:
                        Settings = jObj.ToObject<TSettings>(); return;
                    default:
                        try {
                            Settings = Playnite.SDK.Data.Serialization.FromJson<TSettings>(
                                Playnite.SDK.Data.Serialization.ToJson(value)
                            ) ?? new TSettings();
                        } catch {
                            Settings = new TSettings();
                        }

                        break;
                }
            }
        }
    }
}
