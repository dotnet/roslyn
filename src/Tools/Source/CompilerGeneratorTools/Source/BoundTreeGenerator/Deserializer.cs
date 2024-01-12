// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Collections;
using System.IO;

namespace BoundTreeGenerator
{
    internal class Deserializer
    {
        public T DeserializeElement<T>(Stream stream)
        {
            var doc = XDocument.Load(stream);
            if (doc.Root is { } root)
            {
                return DeserializeElement<T>(root);
            }
            throw new InvalidOperationException();
        }

        public T DeserializeElement<T>(XElement element)
        {
            var obj = DeserializeElement(element, typeof(T));
            return (T)obj;
        }

        public object? DeserializeElement(XElement element, IDictionary<XName, Type> nodeNameToType)
        {
            var nodeName = element.Name;
            if (nodeNameToType.TryGetValue(nodeName, out var type))
            {
                return DeserializeElement(element, type);
            }
            return null;
        }

        private XName GetName(XmlAttributeAttribute attribute, MemberInfo info)
        {
            if (attribute.AttributeName is { Length: > 0 } name)
            {
                return name;
            }
            return info.Name;
        }

        private XName GetName(XmlElementAttribute attribute, MemberInfo info)
        {
            if (attribute.ElementName is { Length: > 0 } name)
            {
                return name;
            }
            return info.Name;
        }

        private Type GetMemberType(MemberInfo info)
        {
            if (info is FieldInfo field)
            {
                return field.FieldType;
            }
            if (info is PropertyInfo p)
            {
                return p.PropertyType;
            }
            throw new NotSupportedException($"{info}");
        }

        private Type GetType(XmlElementAttribute attribute, MemberInfo info)
        {
            if (attribute.Type is { } type)
            {
                return type;
            }
            return GetMemberType(info);
        }

        private Type GetType(XmlAttributeAttribute attribute, MemberInfo info)
        {
            if (attribute.Type is { } type)
            {
                return type;
            }
            return GetMemberType(info);
        }

        private object CreateObjectByType(Type type)
        {
            return Activator.CreateInstance(type) ?? throw new NotSupportedException($"Unable to create node: {type}");
        }

        public object DeserializeElement(XElement element, Type type)
        {
            var obj = CreateObjectByType(type);
            LoadMemberData(element, obj, type);
            return obj;
        }

        protected void LoadMemberData(XElement element, object obj, Type type)
        {
            foreach (var member in type.GetMembers())
            {
                LoadMemberData(element, obj, member);
            }
        }

        protected void LoadMemberData(XElement element, object obj, MemberInfo memberInfo)
        {
            Dictionary<XName, Type>? nameToTypeMap = null;

            foreach (var memberAttribute in memberInfo.GetCustomAttributes())
            {
                if (memberAttribute is XmlAttributeAttribute xmlAttr)
                {
                    var attrName = GetName(xmlAttr, memberInfo);
                    var attrType = GetType(xmlAttr, memberInfo);
                    if (element.Attribute(attrName) is { } attrValue)
                    {
                        AssignAttributeValue(obj, memberInfo, attrValue, attrType);
                    }
                }
                if (memberAttribute is XmlElementAttribute elementAttribute)
                {
                    nameToTypeMap ??= new();
                    var name = GetName(elementAttribute, memberInfo);
                    var elemType = GetType(elementAttribute, memberInfo);

                    nameToTypeMap.Add(name, elemType);
                }
            }

            if (nameToTypeMap is { })
            {
                List<object> childObjList = new();
                foreach (var child in element.Elements())
                {
                    if (DeserializeElement(child, nameToTypeMap) is { } childObj)
                    {
                        childObjList.Add(childObj);
                    }
                }
                AssignElementValue(obj, memberInfo, childObjList);
            }
        }

        private object ConvertSimpleValue(string valueString, Type attrType)
        {
            if (attrType == typeof(string))
            {
                return valueString;
            }
            if (valueString is IConvertible convertible)
            {
                try
                {
                    var converted = convertible.ToType(attrType, null);
                    return converted;
                }
                catch (Exception)
                {

                }
            }

            throw new NotImplementedException();
        }

        private void AssignAttributeValue(object obj, MemberInfo info, XAttribute attribute, Type attrType)
        {
            var strValue = attribute.Value;
            var convertedValue = ConvertSimpleValue(strValue, attrType);

            AssignValue(obj, info, convertedValue);
        }

        private void AssignElementValue(object obj, MemberInfo info, IEnumerable<object> values)
        {
            var collectionType = GetMemberType(info);
            var collectionObject = CreateObjectByType(collectionType);

            if (collectionObject is IList collection)
            {
                foreach (var value in values)
                {
                    collection.Add(value);
                }
            }
            AssignValue(obj, info, collectionObject);
        }

        private void AssignValue(object obj, MemberInfo memberInfo, object value)
        {
            switch (memberInfo)
            {
                case FieldInfo field:
                    field.SetValue(obj, value);
                    break;
                case PropertyInfo property:
                    property.SetValue(obj, value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
