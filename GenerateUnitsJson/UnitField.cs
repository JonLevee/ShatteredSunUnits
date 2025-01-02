namespace GenerateUnitsJson
{

    public class UnitFields : List<UnitField>
    {
        public void Add(
            string path,
            UnitFieldTypeEnum fieldType)
        {
            Add(new UnitField {  
                Path = path,
                FieldType = fieldType
            });
        }
    }

    public class UnitField
    {
        public string Path { get; set; }
        public UnitFieldTypeEnum FieldType { get; set; }
    }

}

