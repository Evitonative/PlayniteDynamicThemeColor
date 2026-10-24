using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DynamicThemeColor.Model {
    /// <summary>
    /// Color scheme definition for a playnite theme.
    /// </summary>
    public class ColorScheme {
        private const int defaultAlpha = 100;

        /// <summary>
        /// DiscoveryPriority of the color scheme
        /// </summary>
        public int DiscoveryPriority { get; set; } = 0;

        /// <summary>
        /// The name of the playnite theme.
        /// </summary>
        [JsonProperty("theme", Required = Required.Always)]
        public string Theme { get; set; }

        /// <summary>
        /// The theme is a fullscreen theme.
        /// </summary>
        [JsonProperty("fullscreen")]
        public bool Fullscreen { get; set; }

        /// <summary>
        /// Author(s) of this theme.
        /// </summary>
        [JsonProperty("author", Required = Required.Always)]
        public List<string> Author { get; set; }

        /// <summary>
        /// Version of this theme.
        /// </summary>
        [JsonProperty("version", Required = Required.Always)]
        public int Version { get; set; }

        /// <summary>
        /// A different scheme this scheme depends on.
        /// </summary>
        [JsonProperty("inherits")]
        [CanBeNull]
        public SchemeDependency Inherits { get; set; }

        /// <summary>
        /// Variables used by scheme.
        /// </summary>
        [JsonProperty("variables")]
        public Dictionary<string, IColorReference> Variables { get; set; } = new Dictionary<string, IColorReference>();

        /// <summary>
        /// Playnite brushes for recoloring the UI.
        /// </summary>
        [JsonProperty("brushes")]
        public Dictionary<string, IBrushDefinition> Brushes { get; set; } = new Dictionary<string, IBrushDefinition>();

        /// <summary>
        /// Validate if the class meets all constraints.
        /// </summary>
        /// <returns>
        /// True if the class matches the required constraints.
        /// </returns>
        public bool Validate(out List<string> errors) {
            errors = new List<string>();
            ValidationContext context = new ValidationContext(this, null, null);
            List<ValidationResult> results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(this, context, results, true);
            if (!isValid) {
                errors.AddRange(results.Select(r => r.ErrorMessage));
            }


            foreach (KeyValuePair<string, IColorReference> kvp in Variables ?? Enumerable.Empty<KeyValuePair<string, IColorReference>>()) {
                ValidateKvp(kvp, "Variable", ref isValid, errors);
            }

            foreach (KeyValuePair<string, IBrushDefinition> kvp in Brushes ?? Enumerable.Empty<KeyValuePair<string, IBrushDefinition>>()) {
                ValidateKvp(kvp, "Brush", ref isValid, errors);
            }

            return isValid;
        }

        private static void ValidateKvp<TKey, TValue>(KeyValuePair<TKey, TValue> kvp, string itemType, ref bool isValid, List<string> errors) {
            if (kvp.Value == null) return;

            List<ValidationResult> innerResults = new List<ValidationResult>();
            ValidationContext innerContext = new ValidationContext(kvp.Value, null, null);

            if (Validator.TryValidateObject(kvp.Value, innerContext, innerResults, true)) return;

            errors.AddRange(innerResults.Select(r => $"{itemType} '{kvp.Key}': {r.ErrorMessage}"));
            isValid = false;
        }

        /// <summary>
        /// Scheme for a theme depended on by another scheme.
        /// </summary>
        public class SchemeDependency {
            /// <summary>
            /// The theme the scheme supports
            /// </summary>
            [JsonProperty("theme", Required = Required.Always)]
            public string Theme { get; set; }

            /// <summary>
            /// The minimum required version of the scheme.
            /// </summary>
            [JsonProperty("min-version")]
            public int? MinVersion { get; set; }

            /// <summary>
            /// Array of authors of the dependent theme.
            /// </summary>
            [JsonProperty("author")]
            [CanBeNull]
            public List<string> Author { get; set; }
        }

        /// <summary>
        /// Reference to a color.
        /// </summary>
        [JsonConverter(typeof(ColorReferenceConverter<HexColor, TransparentColor, MaterialColor, VariableReference>))]
        public interface IColorReference {
            [JsonProperty("type", Required = Required.Always)]
            ColorReferenceType Type { get; }

            [JsonProperty("alpha")]
            [Range(0, 100, ErrorMessage = "Alpha must be between 0 and 100.")]
            int Alpha { get; set; }
        }

        /// <summary>
        /// HEX color
        /// </summary>
        public class HexColor : IColorReference {
            public ColorReferenceType Type => ColorReferenceType.Hex;
            public int Alpha { get; set; } = defaultAlpha;

            [JsonProperty("value", Required = Required.Always)]
            [RegularExpression(@"^#[a-fA-F0-9]{6}$", ErrorMessage = "Invalid HEX color format.")]
            public string Value { get; set; }
        }

        /// <summary>
        /// Transparent color
        /// </summary>
        public class TransparentColor : IColorReference {
            public ColorReferenceType Type => ColorReferenceType.Transparent;
            public int Alpha { get; set; } = 0;
        }

        /// <summary>
        /// Material color
        /// </summary>
        public class MaterialColor : IColorReference {
            public ColorReferenceType Type => ColorReferenceType.Material;
            public int Alpha { get; set; } = defaultAlpha;

            /// <summary>
            /// The Material You palette.
            /// </summary>
            [JsonProperty("palette",  Required = Required.Always)]
            public MaterialPalette Palette { get; set; }

            /// <summary>
            /// The value of the color in the palette. Range 0 - 100.
            /// </summary>
            [JsonProperty("value", Required = Required.AllowNull)]
            [Range(0, 100, ErrorMessage = "Value must be between 0 and 100.")]
            public int Value { get; set; } = 100;
        }

        /// <summary>
        /// Reference to a color variable
        /// </summary>
        public class VariableReference : IColorReference {
            public ColorReferenceType Type => ColorReferenceType.Variable;
            public int Alpha { get; set; } = defaultAlpha;

            /// <summary>
            /// Name of the Variable.
            /// </summary>
            [JsonProperty("variable", Required = Required.Always)]
            [RegularExpression(@"^[a-zA-Z0-9-_]+$", ErrorMessage = "Invalid variable name.")]
            public string Variable { get; set; }
        }

        /// <summary>
        /// Definition of a Brush
        /// </summary>
        [JsonConverter(typeof(ColorReferenceConverter<HexBrush, TransparentBrush, MaterialBrush, VariableBrush>))]
        public interface IBrushDefinition : IColorReference {
            [JsonProperty("optional")]
            [CanBeNull]
            OptionalConfig Optional { get; set; }
        }

        /// <summary>
        /// Hex colored brush
        /// </summary>
        public class HexBrush : HexColor, IBrushDefinition {
            public OptionalConfig Optional { get; set; }
        }

        /// <summary>
        /// Transparent brush
        /// </summary>
        public class TransparentBrush : TransparentColor, IBrushDefinition {
            public OptionalConfig Optional { get; set; }
        }

        /// <summary>
        /// Material color brush
        /// </summary>
        public class MaterialBrush : MaterialColor, IBrushDefinition {
            public OptionalConfig Optional { get; set; }
        }

        /// <summary>
        /// Brush referencing a variable
        /// </summary>
        public class VariableBrush : VariableReference, IBrushDefinition {
            public OptionalConfig Optional { get; set; }
        }

        /// <summary>
        /// Optional config for a brush
        /// </summary>
        public class OptionalConfig {
            /// <summary>
            /// Label of the settings option.
            /// </summary>
            [JsonProperty("label", Required = Required.Always)]
            public string Label { get; set; }

            /// <summary>
            /// Default value of the option.
            /// </summary>
            [JsonProperty("default")]
            public bool Default { get; set; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum MaterialPalette {
            [EnumMember(Value = "Neutral")]
            Neutral,
            [EnumMember(Value = "NeutralVariant")]
            NeutralVariant,
            [EnumMember(Value = "Primary")]
            Primary,
            [EnumMember(Value = "Secondary")]
            Secondary,
            [EnumMember(Value = "Tertiary")]
            Tertiary,
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ColorReferenceType {
            [EnumMember(Value = "hex")]
            Hex,
            [EnumMember(Value = "transparent")]
            Transparent,
            [EnumMember(Value = "material")]
            Material,
            [EnumMember(Value = "variable")]
            Variable,
        }

        private class ColorReferenceConverter <THex, TTransparent, TMaterial, TVariable> : JsonConverter
            where THex : IColorReference
            where TMaterial : IColorReference
            where TVariable : IColorReference {

            public override bool CanWrite => false;

            public override bool CanConvert(Type objectType) {
                return objectType == typeof(IColorReference) ||
                       objectType == typeof(IBrushDefinition);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                if (reader.TokenType == JsonToken.Null) return null;

                JObject jObject = JObject.Load(reader);
                string typeStr = jObject.Value<string>("type")?.ToLowerInvariant();

                Type targetType;
                switch (typeStr) {
                    case "hex":
                        targetType = typeof(THex);
                        break;
                    case "transparent":
                        targetType = typeof(TTransparent);
                        break;
                    case "material":
                        targetType = typeof(TMaterial);
                        break;
                    case "variable":
                        targetType = typeof(TVariable);
                        break;
                    default:
                        throw new JsonSerializationException($"Unknown color reference type: '{typeStr}'");
                }

                object target = existingValue != null && targetType.IsInstanceOfType(existingValue)
                    ? existingValue
                    : Activator.CreateInstance(targetType);

                using (JsonReader objectReader = jObject.CreateReader()) {
                    serializer.Populate(objectReader, target);
                }

                return target;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                serializer.Serialize(writer, value);
            }
        }
    }
}
