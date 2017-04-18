using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TentacleSoftware.XmlRpc.Core
{
    public class ValueContext
    {
        public Type Type { get; private set; }

        public Type CollectedType { get; private set; }

        public Dictionary<string, PropertyInfo> Properties { get; private set; }

        public object Value { get; private set; }

        public MethodInfo Set { get; set; }

        public ValueContext(Type valueType)
        {
            Type = valueType;
            Properties = new Dictionary<string, PropertyInfo>();

            if (valueType != typeof(string) && typeof(IList).IsAssignableFrom(valueType))
            {
                // Find the type our collection contains, which we'll use this to create instances of any Structs in this collection
                if (valueType.IsGenericType)
                {
                    // If generic collection, what's our T?
                    CollectedType = valueType.GetGenericArguments().FirstOrDefault();

                    // Hope that this is a generic collection of some kind
                    Value = Activator.CreateInstance(valueType);
                }
                else if (valueType.IsArray)
                {
                    // If array, what's our T[]?
                    CollectedType = valueType.GetElementType();

                    // We treat arrays differently, because they're ILists but they throw when .Add() is called
                    // We'll back this array with a List<T> and call .ToArray() later
                    Value = Activator.CreateInstance(typeof(List<>).MakeGenericType(CollectedType));
                }
                else
                {
                    throw new ArgumentException($"{valueType} must be an IList that is either an array or generic collection", nameof(valueType));
                }
            }
            else
            {
                // Find our public properties
                foreach (PropertyInfo property in valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (property.GetCustomAttribute<XmlRpcIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    XmlRpcMemberAttribute xmlRpcMember = property.GetCustomAttribute<XmlRpcMemberAttribute>();

                    if (xmlRpcMember != null)
                    {
                        Properties.Add(xmlRpcMember.Name, property);
                    }
                    else
                    {
                        Properties.Add(property.Name, property);
                    }
                }

                Value = Activator.CreateInstance(valueType);
            }
        }
    }
}
