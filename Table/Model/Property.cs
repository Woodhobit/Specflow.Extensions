using System.Collections.Generic;

namespace Table.Model
{
    internal class Property
    {
        public string PropertyPath { get; set; }

        public List<PropertyPart> Parts { get; set; }

        public string RawStringValue { get; set; }
    }
}
