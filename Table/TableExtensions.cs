using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Table.Model;
using Table.Service;

namespace Table
{
    public static class TableExtensions
    {
        public static IEnumerable<T> CreateDeepInstanceOfCollection<T>(this TechTalk.SpecFlow.Table table, Func<T> objectCreator = null)
        {
            var rowCount = table.Rows.Count;
            if (rowCount < 1)
            {
                return default(IEnumerable<T>);
            }

            var collection = Activator.CreateInstance<List<T>>();
            var headers = table.Header.ToArray();

            for (var i = 0; i < rowCount; i++)
            {
                var properties = GetProperties<T>(headers, table.Rows[i].Values.ToArray());
                var instance = objectCreator != null ? objectCreator() : Activator.CreateInstance<T>();

                foreach (var property in properties)
                {
                    PropertyInstanceService.BuildProperty(property, 0, instance);
                }

                collection.Add(instance);
            }

            return collection;
        }

        public static T CreateDeepInstance<T>(this TechTalk.SpecFlow.Table table, Func<T> objectCreator = null)
        {
            var rowCount = table.Rows.Count;
            if (rowCount == 0 || table.Rows.Count > 1)
            {
                return default(T);
            }

            var instance = objectCreator != null ? objectCreator() : Activator.CreateInstance<T>();
            var properties = GetProperties<T>(table);

            foreach (var property in properties)
            {
                PropertyInstanceService.BuildProperty(property, 0, instance);
            }

            return instance;
        }

        private static List<Property> GetProperties<T>(TechTalk.SpecFlow.Table table)
        {
            var headers = table.Header.ToArray();
            var row = table.Rows.FirstOrDefault()?.Values.ToArray();

            return GetProperties<T>(headers, row);
        }

        private static List<Property> GetProperties<T>(string[] headers, string[] row)
        {
            return headers.Select(
                (t, i) => new Property
                {
                    PropertyPath = t,
                    Parts = GetPropertyParts(t),
                    RawStringValue = row[i]
                }).ToList();
        }

        private static List<PropertyPart> GetPropertyParts(string propertyPath)
        {
            var parts = new List<PropertyPart>();
            var stringBuilder = new StringBuilder();

            foreach (var character in propertyPath)
            {
                if (character == '.')
                {
                    var property = new PropertyPart { Path = stringBuilder.ToString() };
                    UpdateProperty(property);
                    parts.Add(property);
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.Append(character);
                }
            }

            var prop = new PropertyPart { Path = stringBuilder.ToString() };
            UpdateProperty(prop);
            parts.Add(prop);

            return parts;
        }

        private static void UpdateProperty(PropertyPart propertyPart)
        {
            if (propertyPart.Path.Contains("["))
            {
                var collect = false;
                var sb = new StringBuilder();

                for (var i = 0; i < propertyPart.Path.Length; i++)
                {
                    if (propertyPart.Path[i] == ']')
                    {
                        collect = false;
                    }

                    if (collect)
                    {
                        sb.Append(propertyPart.Path[i]);
                    }

                    if (propertyPart.Path[i] != '[')
                    {
                        continue;
                    }

                    propertyPart.Name = propertyPart.Path.Substring(0, i);
                    collect = true;
                }

                var result = sb.ToString();
                propertyPart.Key = result.Replace("\"", string.Empty);
            }
            else
            {
                propertyPart.Name = propertyPart.Path;
            }
        }
    }
}