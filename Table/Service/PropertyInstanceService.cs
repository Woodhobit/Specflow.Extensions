using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Table.Model;

namespace Table.Service
{
    internal static class PropertyInstanceService
    {
        public static void BuildProperty(Property property, int partIndex, object parent)
        {
            var propertyInfo = parent.GetType().GetProperty(property.Parts[partIndex].Name);

            if (propertyInfo == null)
            {
                return;
            }

            if (!propertyInfo.CanWrite)
            {
                return;
            }

            if (IsSimpleType(propertyInfo))
            {
                if (string.IsNullOrEmpty(propertyInfo.PropertyType.FullName))
                {
                    return;
                }

                if (propertyInfo.PropertyType.FullName.Contains("Nullable") && string.IsNullOrEmpty(property.RawStringValue))
                {
                    propertyInfo.SetValue(parent, null);
                    return;
                }

                var simpleTypeValue = GetSimpleTypeValue(property.RawStringValue, propertyInfo.PropertyType);
                propertyInfo.SetValue(parent, simpleTypeValue);

                return;
            }

            if (IsEnum(propertyInfo))
            {
                object enumValue = null;
                if (string.IsNullOrEmpty(propertyInfo.PropertyType.FullName))
                {
                    return;
                }

                if (propertyInfo.PropertyType.FullName.Contains("Nullable"))
                {
                    var u = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
                    if ((u != null) && u.IsEnum)
                    {
                        if (string.IsNullOrEmpty(property.RawStringValue))
                        {
                            propertyInfo.SetValue(parent, enumValue);
                            return;
                        }

                        enumValue = Enum.Parse(u, property.RawStringValue, true);
                    }
                }
                else
                {
                    enumValue = Enum.Parse(propertyInfo.PropertyType, property.RawStringValue, true);
                }

                propertyInfo.SetValue(parent, enumValue);

                return;
            }

            if (property.Parts[partIndex].Type == PropertyType.Standard)
            {
                var childInstance = propertyInfo.GetValue(parent);
                if (childInstance == null)
                {
                    childInstance = Activator.CreateInstance(propertyInfo.PropertyType);
                    propertyInfo.SetValue(parent, childInstance);
                }

                BuildProperty(property, partIndex + 1, childInstance);

                return;
            }

            if (propertyInfo.PropertyType.IsArray)
            {
                var propertyElementType = propertyInfo.PropertyType.GetElementType();
                var childInstance = (IList)propertyInfo.GetValue(parent);

                if (childInstance == null)
                {
                    childInstance = Array.CreateInstance(propertyElementType, 0);
                    propertyInfo.SetValue(parent, childInstance);
                }

                if (IsSimpleType(propertyElementType))
                {
                    var newArray = Array.CreateInstance(propertyElementType, childInstance.Count + 1);
                    Array.Copy((Array)childInstance, newArray, childInstance.Count);
                    childInstance = newArray;
                    childInstance[childInstance.Count - 1] = GetSimpleTypeValue(property.RawStringValue, propertyElementType);
                    propertyInfo.SetValue(parent, childInstance);
                }
                else
                {
                    object item;
                    var indexPosition = int.Parse(property.Parts[partIndex].Key);

                    if (childInstance.Count > indexPosition)
                    {
                        item = childInstance[indexPosition];
                    }
                    else
                    {
                        item = Activator.CreateInstance(propertyElementType);
                        var newArray = Array.CreateInstance(propertyElementType, childInstance.Count + 1);
                        Array.Copy((Array)childInstance, newArray, childInstance.Count);
                        childInstance = newArray;
                        childInstance[childInstance.Count - 1] = item;
                    }

                    propertyInfo.SetValue(parent, childInstance);
                    BuildProperty(property, partIndex + 1, item);
                }

                return;
            }

            if (IsCollection(propertyInfo))
            {
                var childInstance = propertyInfo.GetValue(parent);
                var genericTypeArgument = propertyInfo.PropertyType.GenericTypeArguments[0];

                if (childInstance == null)
                {
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(genericTypeArgument);
                    childInstance = Activator.CreateInstance(constructedListType);
                    propertyInfo.SetValue(parent, childInstance);
                }

                if (IsSimpleType(genericTypeArgument))
                {
                    ((IList)childInstance).Add(GetSimpleTypeValue(property.RawStringValue, genericTypeArgument));
                }
                else
                {
                    object item;
                    var indexPosition = int.Parse(property.Parts[partIndex].Key);

                    if (((IList)childInstance).Count > indexPosition)
                    {
                        item = ((IList)childInstance)[indexPosition];
                    }
                    else
                    {
                        item = Activator.CreateInstance(genericTypeArgument);
                        ((IList)childInstance).Add(item);
                    }

                    BuildProperty(property, partIndex + 1, item);
                }

                return;
            }

            if (IsDictionary(propertyInfo))
            {
                var childInstance = propertyInfo.GetValue(parent);
                var genericTypeKeyArgument = propertyInfo.PropertyType.GenericTypeArguments[0];
                var genericTypeValueArgument = propertyInfo.PropertyType.GenericTypeArguments[1];

                if (childInstance == null)
                {
                    var dictionaryType = typeof(Dictionary<,>);
                    var constructedListType = dictionaryType.MakeGenericType(genericTypeKeyArgument, genericTypeValueArgument);
                    childInstance = Activator.CreateInstance(constructedListType);
                    propertyInfo.SetValue(parent, childInstance);
                }

                if (IsSimpleType(genericTypeValueArgument))
                {
                    ((IDictionary)childInstance).Add(
                        GetSimpleTypeValue(property.Parts[partIndex].Key, genericTypeKeyArgument),
                        GetSimpleTypeValue(property.RawStringValue, genericTypeValueArgument));
                }
                else
                {
                    object item;
                    var key = int.Parse(property.Parts[partIndex].Key);

                    if (((IDictionary)childInstance).Count > key)
                    {
                        item = ((IDictionary)childInstance)[key];
                    }
                    else
                    {
                        item = Activator.CreateInstance(genericTypeValueArgument);
                        ((IDictionary)childInstance).Add(
                            GetSimpleTypeValue(property.Parts[partIndex].Key, genericTypeKeyArgument),
                            item);
                    }

                    BuildProperty(property, partIndex + 1, item);
                }
            }
        }

        internal static object GetInstancePropertyValue(Property property, int partIndex, object parent)
        {
            var propertyPart = property.Parts[partIndex];
            var propertyInfo = parent.GetType().GetProperty(propertyPart.Name);
            var propertyValue = propertyInfo.GetValue(parent);

            if (partIndex == property.Parts.Count - 1 && propertyPart.Type == PropertyType.Standard)
            {
                return propertyValue;
            }

            if (partIndex == property.Parts.Count - 1 && propertyPart.Type == PropertyType.KeyedOrIndexer)
            {
                if (propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IList<>).FullName).Length > 0)
                {
                    return ((IList)propertyValue)[int.Parse(propertyPart.Key)];
                }

                if (propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IDictionary<,>).FullName).Length > 0)
                {
                    return ((IDictionary)propertyValue)[propertyPart.Key];
                }
            }
            else if (partIndex < property.Parts.Count - 1 && propertyPart.Type == PropertyType.KeyedOrIndexer)
            {
                if (propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IList<>).FullName).Length > 0)
                {
                    return GetInstancePropertyValue(property, partIndex + 1, ((IList)propertyValue)[int.Parse(propertyPart.Key)]);
                }

                if (propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IDictionary<,>).FullName).Length > 0)
                {
                    return GetInstancePropertyValue(property, partIndex + 1, ((IDictionary)propertyValue)[propertyPart.Key]);
                }
            }

            return GetInstancePropertyValue(property, partIndex + 1, propertyValue);
        }

        private static bool MyInterfaceFilter(Type typeObj, Object criteriaObj)
        {
            return typeObj.ToString().Contains(criteriaObj.ToString());
        }

        private static bool IsSimpleType(PropertyInfo property)
        {
            if (property == null)
            {
                return false;
            }

            if (IsEnum(property))
            {
                return false;
            }

            return property.PropertyType.IsPrimitive ||
                   property.PropertyType.IsValueType ||
                   property.PropertyType == typeof(string) ||
                   property.PropertyType.FullName.Contains("Nullable");
        }

        private static bool IsEnum(PropertyInfo property)
        {
            if (property.PropertyType.IsEnum)
            {
                return true;
            }

            if (string.IsNullOrEmpty(property.PropertyType.FullName))
            {
                return false;
            }

            if (!property.PropertyType.FullName.Contains("Nullable"))
            {
                return false;
            }

            var u = Nullable.GetUnderlyingType(property.PropertyType);

            return u != null && u.IsEnum;
        }

        private static bool IsSimpleType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return type.IsPrimitive ||
                   type.IsValueType ||
                   type == typeof(string) ||
                   type.FullName.Contains("Nullable");
        }

        private static object GetSimpleTypeValue(string value, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(Guid) || type == typeof(Guid?))
            {
                return new Guid(value.PadRight(32,'0'));
            }

            if (type != typeof(DateTimeOffset))
            {
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            var dateTime = (DateTime)Convert.ChangeType(value,
                typeof(DateTime), CultureInfo.InvariantCulture);

            return new DateTimeOffset(dateTime);
        }

        private static bool IsCollection(PropertyInfo propertyInfo)
        {
            return (propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IList<>).FullName).Length > 0) ||
                                (propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IEnumerable).FullName).Length > 0);
        }

        private static bool IsDictionary(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType.FindInterfaces(MyInterfaceFilter, typeof(IDictionary<,>).FullName).Length > 0;
        }
    }
}
