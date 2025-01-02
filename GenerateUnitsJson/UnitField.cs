namespace GenerateUnitsJson
{

    public class UnitFields : Dictionary<string, UnitFields>
    {
        public UnitField? UnitField { get; set; }

        public void Add(
            string path,
            UnitFieldTypeEnum fieldType)
        {
            var pathParts = path.Split('/').ToList();
            AddUnitField(path, path.Split('/').ToList(), 0, fieldType);
        }

        private void AddUnitField(
            string path,
            List<string> pathParts,
            int pathIndex,
            UnitFieldTypeEnum fieldType)
        {
            if (pathIndex >= pathParts.Count)
            {
                UnitField = new UnitField
                {
                    Path = path,
                    FieldType = fieldType,
                };
                return;
            }
            var childKey = pathParts[pathIndex];
            if (!TryGetValue(childKey, out var child))
            {
                child = new UnitFields();
                Add(childKey, child);
            }
            child.AddUnitField(path, pathParts, pathIndex + 1, fieldType);
        }
    }

    public class UnitField
    {
        public required string Path { get; set; }
        public required UnitFieldTypeEnum FieldType { get; set; }

        public int Seen { get; set; } = 0;
    }

}

