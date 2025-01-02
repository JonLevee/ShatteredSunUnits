using NLua;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenerateUnitsJson.Utils
{
    public class JsonHelper
    {
        /// <summary>
        /// The max expected number of path parts that are used to collect unit data.
        /// </summary>
        public static readonly int ExpectedMaxGroups = 3;

        /// <summary>
        /// All possible tech tiers
        /// </summary>
        public static readonly string[] TECHTIERS = Enumerable.Range(1, 9).Select(i => $"TECH{i}").ToArray();

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
                        {
                            new LuaTableConverter(),
                            new JsonStringEnumConverter<UnitFieldTypeEnum>()
                        },
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        public class LuaTableConverter : JsonConverter<LuaTable>
        {
            public override LuaTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, LuaTable value, JsonSerializerOptions options)
            {
                var text = string.Empty;
                var isList = Enumerable
                    .Range(1, value.Keys.Count)
                    .Zip(value.Keys.Cast<object>(), (i, k) => k is long && Equals((long)k, (long)i))
                    .All(r => r);
                if (isList)
                {
                    var list = value.Values.Cast<object>().ToArray();
                    text = JsonSerializer.Serialize(list, options);
                }
                else
                {
                    var dictionary = value.Keys.Cast<object>().ToDictionary(k => k, k => value[k]);
                    text = JsonSerializer.Serialize(dictionary, options);
                }
                writer.WriteRawValue(text);
            }
        }

        public static JsonNode ConvertLuaTableToJson(LuaTable luaTable)
        {
            var json = JsonSerializer.Serialize(luaTable, JsonHelper.JsonOptions);
            var jsonNode = JsonSerializer.Deserialize<JsonNode>(json, JsonHelper.JsonOptions);
            Debug.Assert(jsonNode != null);
            return jsonNode;
        }
    }
}
