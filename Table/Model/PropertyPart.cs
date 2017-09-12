namespace Table.Model
{
    internal class PropertyPart
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string Key { get; set; }

        public PropertyType Type => Key == null ? PropertyType.Standard : PropertyType.KeyedOrIndexer;
    }
}
