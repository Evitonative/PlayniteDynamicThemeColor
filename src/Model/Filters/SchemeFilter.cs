using System;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Model.Filters {
    public abstract class SchemeFilter {
        /// <summary>
        /// A unique but never changing id for this filter.
        ///
        /// Use <code>Guid.Parse("{UUIDv4}")</code> to set it.
        ///
        /// You can use https://www.uuidgenerator.net/version4 to generate it.
        /// </summary>
        public abstract Guid Id { get; }

        /// <summary>
        /// The name of the filter
        ///
        /// This will be shown to the user.
        /// </summary>
        [NotNull]
        public abstract string Name { get; }

        /// <summary>
        /// The author of the filter.
        ///
        /// This will be shown to the user.
        /// </summary>
        [NotNull]
        public abstract string Author { get; }

        /// <summary>
        /// The version of the filter.
        ///
        /// Currently only shown to the user.
        /// May later be used for validation or update logic.
        /// </summary>
        public abstract int Version { get; }

        /// <summary>
        /// Description of what the filter does.
        ///
        /// This will be shown to the user.
        /// </summary>
        [CanBeNull]
        public abstract string Description { get; }

        [CanBeNull]
        internal virtual object SettingsObject {
            get => null;
            // ReSharper disable once ValueParameterNotUsed
            set { }
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
        public virtual uint ApplySeedFilters(
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
        public virtual void ApplyPaletteFilters(
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
        public virtual uint ApplyBrushFilters(
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
        protected TSettings Settings { get; private set; } = new TSettings();

        internal override object SettingsObject {
            get => Settings;
            set {
                switch (value)
                {
                    case null:
                        Settings = new TSettings(); return;
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
