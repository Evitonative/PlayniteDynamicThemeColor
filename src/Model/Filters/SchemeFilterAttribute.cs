using System;
using JetBrains.Annotations;

namespace DynamicThemeColor.Model.Filters {
    public class SchemeFilterAttribute : Attribute {
        /// <summary>
        /// A unique but never changing id for this filter.
        ///
        /// Use <code>Guid.Parse("{UUIDv4}")</code> to set it.
        ///
        /// You can use https://www.uuidgenerator.net/version4 to generate it.
        /// </summary>
        public Guid FilterId;

        /// <summary>
        /// The name of the filter
        ///
        /// This will be shown to the user.
        /// </summary>
        [NotNull] public string Name;

        /// <summary>
        /// Description of what the filter does.
        ///
        /// This will be shown to the user.
        /// </summary>
        [CanBeNull] public string Description;

        /// <summary>
        /// The author of the filter.
        /// If null the name in the attribute will be used.
        ///
        /// This will be shown to the user.
        /// </summary>
        [NotNull] public string Author;

        /// <summary>
        /// The version of the filter.
        ///
        /// Currently only shown to the user.
        /// May later be used for validation or update logic.
        /// </summary>
        public int Version;

        public SchemeFilterAttribute(
            string filterId,
            [NotNull] string name,
            [NotNull] string author,
            int version,
            [CanBeNull] string description = null) {
            FilterId = Guid.Parse(filterId);
            Name = name;
            Description = description;
            Author = author;
            Version = version;
        }
    }
}
