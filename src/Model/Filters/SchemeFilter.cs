using System;
using System.Collections.Generic;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Model.Filters {
    public abstract class SchemeFilter {
        protected readonly ILogger Logger = LogManager.GetLogger();

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

        [CanBeNull]
        internal virtual object SettingsObject {
            get => null;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        /// <summary>
        /// Apply a color to the seed color for theming before palette generation.
        /// </summary>
        /// <param name="seedArgb">
        /// The current seed color in ARGB format.
        ///
        /// It may have been already modified by other filters beforehand.
        /// Other filters may be applied after.
        /// </param>
        /// <param name="game">
        /// The current selected game.
        ///
        /// May be null if no game is selected.
        /// </param>
        /// <returns>
        /// The new seed color in ARGB format.
        /// </returns>
        public virtual uint ApplySeedFilters(uint seedArgb, [CanBeNull] Game game) {
            return seedArgb;
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
        /// <param name="game">
        /// The current selected game.
        ///
        /// May be null if no game is selected.
        /// </param>
        public virtual void ApplyPaletteFilters(ThemePalette palette, [CanBeNull] Game game) {
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
        /// <param name="game">
        /// The current selected game.
        ///
        /// May be null if no game is selected.
        /// </param>
        /// <returns>
        /// The new brush color in ARGB format.
        /// </returns>
        public virtual uint ApplyBrushFilters(string brushKey, uint brushArgb, [CanBeNull] Game game) {
            return brushArgb;
        }
    }

    public abstract class SchemeFilter<TSettings> : SchemeFilter where TSettings : new() {
        private TSettings settings = new TSettings();

        protected TSettings Settings {
            get => settings;
            set => settings = value;
        }

        internal override object SettingsObject {
            get => Settings;
            set => Settings = (TSettings)value;
        }
    }
}
