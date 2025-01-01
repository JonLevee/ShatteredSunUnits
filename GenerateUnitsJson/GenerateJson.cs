using NLua;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using GenerateUnitsJson.Utils;
using GenerateUnitsJson.Extensions;
using System.Xml.Linq;

namespace GenerateUnitsJson
{

    internal class GenerateJson
    {
        private string luaRoot = string.Empty;

        private static UnitFields unitFields = new UnitFields() {
            {"adjacency", UnitFieldTypeEnum.String},
            {"collisionInfo", UnitFieldTypeEnum.Ignore},
            {"construction/buildableOnResources", UnitFieldTypeEnum.Ignore},
            {"construction/buildPower", UnitFieldTypeEnum.Long},
            {"construction/canBuild", UnitFieldTypeEnum.String},
            {"construction/range", UnitFieldTypeEnum.Double},
            {"construction/rollOffPoints", UnitFieldTypeEnum.Ignore},
            {"construction/upgradesTo", UnitFieldTypeEnum.String},
            {"defence/health/max", UnitFieldTypeEnum.Long},
            {"defence/health/regen", UnitFieldTypeEnum.Long},
            {"defence/health/value", UnitFieldTypeEnum.Ignore},
            {"defence/shields/max", UnitFieldTypeEnum.Long},
            {"defence/shields/name", UnitFieldTypeEnum.String},
            {"defence/shields/rechargeTime", UnitFieldTypeEnum.Long},
            {"defence/shields/regen", UnitFieldTypeEnum.Long},
            {"defence/shields/regenDelay", UnitFieldTypeEnum.Long},
            {"economy/buildTime", UnitFieldTypeEnum.Long},
            {"economy/cost/alloys", UnitFieldTypeEnum.Long},
            {"economy/cost/energy", UnitFieldTypeEnum.Long},
            {"economy/maintenanceConsumption/energy", UnitFieldTypeEnum.Long},
            {"economy/production/alloys", UnitFieldTypeEnum.Long},
            {"economy/production/energy", UnitFieldTypeEnum.Long},
            {"economy/storage/alloys", UnitFieldTypeEnum.Long},
            {"economy/storage/energy", UnitFieldTypeEnum.Long},
            {"footprint", UnitFieldTypeEnum.Ignore},
            {"general/class", UnitFieldTypeEnum.StringArray},
            {"general/displayName", UnitFieldTypeEnum.String},
            {"general/icon", UnitFieldTypeEnum.Ignore},
            {"general/iconUI", UnitFieldTypeEnum.Image},
            {"general/iconUIBuildSortPriority", UnitFieldTypeEnum.Ignore},
            {"general/name", UnitFieldTypeEnum.String},
            {"general/orders", UnitFieldTypeEnum.StringArray},
            {"general/tpId", UnitFieldTypeEnum.String},
            {"intel/radarRadius", UnitFieldTypeEnum.Long},
            {"intel/visionRadius", UnitFieldTypeEnum.Long},
            {"isFactory", UnitFieldTypeEnum.Ignore},
            {"movement/acceleration", UnitFieldTypeEnum.Double},
            {"movement/air", UnitFieldTypeEnum.Bool},
            {"movement/airHover", UnitFieldTypeEnum.Ignore},
            {"movement/animClips", UnitFieldTypeEnum.Ignore},
            {"movement/mass", UnitFieldTypeEnum.Ignore},
            {"movement/minSpeed", UnitFieldTypeEnum.Double},
            {"movement/rotationSpeed", UnitFieldTypeEnum.Double},
            {"movement/sortOrder", UnitFieldTypeEnum.Ignore},
            {"movement/speed", UnitFieldTypeEnum.Double},
            {"movement/type", UnitFieldTypeEnum.String},
            {"skirtSize", UnitFieldTypeEnum.Ignore},
            {"tags", UnitFieldTypeEnum.StringArray},
            {"transport/storage", UnitFieldTypeEnum.Ignore},
            {"turrets", UnitFieldTypeEnum.Ignore},
            {"visuals", UnitFieldTypeEnum.Ignore},

        };

        // need to add faction and tier


        public void GenerateJsonData()
        {
            var steamInfo = new SteamInfo();
            var steamRoot = steamInfo.GetRoot();
            luaRoot = Path.Combine(steamRoot, @"engine\LJ\lua");
            GenerateData();

        }

        private Dictionary<string,string> factionLookup = new Dictionary<string,string>();
        private Dictionary<string, bool> unitEnabled = new Dictionary<string, bool>();

        public void GenerateData()
        {
            using (LuaHelper.GetLuaTable(luaRoot, "common/systems/factions.lua", "FactionsData", out LuaTable table))
            {
                foreach (var item in table.Values.Cast<LuaTable>())
                {
                    Debug.Assert(item != null);
                    factionLookup.Add(item["tpLetter"].ToStringNullSafe(), item["name"].ToStringNullSafe());
                }
            }

            using (LuaHelper.GetLuaTable(luaRoot, "common/units/availableUnits.lua", "AvailableUnits", out LuaTable table))
            {
                foreach (string key in table.Keys)
                {
                    unitEnabled.Add(key, (bool)table[key]);
                }
            }

            var data = new object();
            foreach (var file in Directory.GetFiles(Path.Combine(luaRoot, "common/units/unitsTemplates"), "*.santp", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(luaRoot.Length + 1);
                using (LuaHelper.GetLuaTable(luaRoot, relativePath, "UnitTemplate", out LuaTable table))
                {
                    // converting the table to json format makes it easier to process
                    var jsonNode = JsonHelper.ConvertLuaTableToJson(table);
                    foreach (var unitField in unitFields.Where(uf=>uf.FieldType != UnitFieldTypeEnum.Ignore))
                    {
                        var pathParts = unitField.Path.Split('/').ToList();
                        if (TryGetField(jsonNode, pathParts, 0))
                        {

                        }
                    }
                }
            }

            var outputPath = "../../GenerateUnitsJson.json";
            File.WriteAllText(outputPath, JsonSerializer.Serialize(data, JsonHelper.JsonOptions));

        }

        private bool TryGetField(JsonNode node, List<string> pathParts, int pathIndex)
        {
            for (; node != null && pathIndex < pathParts.Count; pathIndex++)
            {
                var pathPart = pathParts[pathIndex];
                if (node is JsonObject jObject)
                {
                    node = jObject[pathPart].ToNullSafe();
                }
                else if (node is JsonArray array)
                {
                    var save = pathParts[pathIndex - 1];
                    for (var i = 0; i < array.Count; ++i)
                    {
                        pathParts[pathIndex - 1] = $"{save}[{i}]";
                        Add(ld, unit, array[i].ToNullSafe(), createField, pathParts, pathIndex);
                    }
                    return;
                }

            }

            return false;
        }

    }
}

