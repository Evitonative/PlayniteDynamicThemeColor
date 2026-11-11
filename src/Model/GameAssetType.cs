using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DynamicThemeColor.Model {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameAssetType {
        Background,
        Cover,
        Icon,
    }
}
