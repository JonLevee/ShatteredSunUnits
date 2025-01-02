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
            {"defence/shields/*/max", UnitFieldTypeEnum.Long},
            {"defence/shields/*/name", UnitFieldTypeEnum.String},
            {"defence/shields/*/rechargeTime", UnitFieldTypeEnum.Long},
            {"defence/shields/*/regen", UnitFieldTypeEnum.Long},
            {"defence/shields/*/regenDelay", UnitFieldTypeEnum.Long},
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
        private Dictionary<string, string> factionLookup = new Dictionary<string, string>();
        private Dictionary<string, bool> unitEnabled = new Dictionary<string, bool>();


        public void GenerateJsonData()
        {
            var steamInfo = new SteamInfo();
            var steamRoot = steamInfo.GetRoot();
            luaRoot = Path.Combine(steamRoot, @"engine\LJ\lua");
            GenerateData();

        }

        public void GenerateData()
        {
            var data = new JsonObject();
            var factions = (JsonArray)(data["factions"] = new JsonArray());

            using (LuaHelper.GetLuaTable(luaRoot, "common/units/availableUnits.lua", "AvailableUnits", out LuaTable table))
            {
                foreach (string key in table.Keys)
                {
                    unitEnabled.Add(key, (bool)table[key]);
                }
            }

            using (LuaHelper.GetLuaTable(luaRoot, "common/systems/factions.lua", "FactionsData", out LuaTable table))
            {
                foreach (var item in table.Values.Cast<LuaTable>())
                {
                    Debug.Assert(item != null);
                    var name = item["name"].ToStringNullSafe();
                    factionLookup.Add(item["tpLetter"].ToStringNullSafe(), name);
                    factions.Add(name);
                }
            }


            var units = (JsonArray)(data["units"] = new JsonArray());

            foreach (var file in Directory.GetFiles(Path.Combine(luaRoot, "common/units/unitsTemplates"), "*.santp", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(luaRoot.Length + 1);
                using (LuaHelper.GetLuaTable(luaRoot, relativePath, "UnitTemplate", out LuaTable table))
                {
                    // converting the table to json format makes it easier to process
                    var jsonNode = JsonHelper.ConvertLuaTableToJson(table);
                    var unit = GetUnitAsDictionary(jsonNode);
                    units.Add(unit);
                }
            }

            var outputPath = "../../../GenerateUnitsJson.json";
            var fullPath = Path.GetFullPath(outputPath);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(data, JsonHelper.JsonOptions));

        }

        private Dictionary<string, string> GetUnitAsDictionary(JsonNode node)
        {
            var unit = new Dictionary<string, string>();

            RecursiveMerge(unit, node, unitFields);
            var tpId = unit["general/tpId"];
            unit["enabled"] = (unitEnabled.TryGetValue(tpId, out var enabled) ? enabled : false).ToString();
            unit["faction"] =factionLookup[tpId.Substring(0,2)];
            return unit;
        }

        private void RecursiveMerge(Dictionary<string, string> unit, JsonNode? node, UnitFields unitFields)
        {
            if (node == null)
                return;
            if (unitFields.UnitField != null)
            {
                var unitField = unitFields.UnitField;
                ++unitField.Seen;
                var formattedPath = node.GetPath().Replace("$.", string.Empty).Replace(".","/");
                switch (unitField.FieldType)
                {
                    case UnitFieldTypeEnum.Ignore:
                        break;
                    case UnitFieldTypeEnum.String:
                        unit[formattedPath] = node.GetValue<string>();
                        break;
                    case UnitFieldTypeEnum.Double:
                        unit[formattedPath] = node.GetValue<double>().ToString(); ;
                        break;
                    case UnitFieldTypeEnum.Long:
                        unit[formattedPath] = node.GetValue<long>().ToString();
                        break;
                    case UnitFieldTypeEnum.StringArray:
                        if (node is JsonArray nodeArray)
                        {
                            unit[formattedPath] = string.Join(", ", nodeArray.Select(n => n.GetValue<string>()));
                            break;
                        }
                        if (node is JsonObject nodeObject)
                        {
                            var list = new List<string>();
                            foreach(var property in nodeObject)
                            {
                                if (((bool?)property.Value).GetValueOrDefault())
                                {
                                    list.Add(property.Key);
                                }
                            }
                            unit[formattedPath] = string.Join(", ", list);
                            break;
                        }
                        throw new InvalidOperationException();
                    case UnitFieldTypeEnum.Image:
                        unit[formattedPath] = node.GetValue<string>();
                        break;
                    case UnitFieldTypeEnum.Bool:
                        unit[formattedPath] = node.GetValue<bool>().ToString(); ;
                        break;
                }

            }
            foreach (var childKey in unitFields.Keys)
            {
                if (node is JsonObject jsonObject)
                {
                    RecursiveMerge(unit, jsonObject[childKey], unitFields[childKey]);
                    continue;
                }

                if (node is JsonArray jsonArray && childKey == "*")
                {
                    foreach(var child in jsonArray)
                    {
                        RecursiveMerge(unit, child, unitFields[childKey]);
                    }
                    continue;
                }
                throw new InvalidOperationException();
            }
        }

        private void MergeUnitData(Dictionary<string, string> unit, JsonNode? node, UnitField unitField, Queue<string> pathParts)
        {
            if (node == null || !pathParts.TryDequeue(out var pathKey))
                return;
            var child = node[pathKey];
            MergeUnitData(unit, child, unitField, pathParts);

        }
    }

}

