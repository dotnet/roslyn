using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    public class PrimitiveOptionSerializer : IOptionSerializer
    {
        private const string OptionsString = "Options";
        private const string OptionString = "Option";

        private const string NameString = "Name";
        private const string TypeString = "Type";
        private const string ValueString = "Value";

        private const string NullString = "Null";

        private static readonly HashSet<Type> supportedTypes = new HashSet<Type>()
        {
             typeof(bool), 
             typeof(byte), 
             typeof(char), 
             typeof(decimal),
             typeof(double),
             typeof(float), 
             typeof(int), 
             typeof(long), 
             typeof(sbyte),
             typeof(short), 
             typeof(string),
             typeof(uint), 
             typeof(ulong),
             typeof(ushort),
        };

        private static readonly Dictionary<Type, string> typeToString = supportedTypes.ToDictionary(
            type => type,
            type => type.FullName);

        private static readonly Dictionary<string, Type> stringToType = supportedTypes.ToDictionary(
            type => type.FullName);

        private static readonly ConcurrentDictionary<Type, Func<string, object>> typeToConverter =
            new ConcurrentDictionary<Type, Func<string, object>>();

        public string Serialize(KeyValuePair<string, object>[] data)
        {
            if (!ContainsOnlySupportedTypes(data))
            {
                throw new ArgumentException();
            }

            var xmlOptions = new XElement(
                OptionsString,
                from pair in data
                select new XElement(
                    OptionString,
                    new XAttribute(NameString, pair.Key),
                    new XAttribute(TypeString, GetXmlType(pair.Value)),
                    new XAttribute(ValueString, GetXmlContent(pair.Value))));

            return xmlOptions.ToString(SaveOptions.None);
        }

        private object GetXmlContent(object value)
        {
            return value == null
                ? NullString
                : value;
        }

        private string GetXmlType(object value)
        {
            return value == null
                ? NullString
                : typeToString[value.GetType()];
        }

        private bool ContainsOnlySupportedTypes(KeyValuePair<string, object>[] data)
        {
            return data.All(IsSupported);
        }

        private bool IsSupported(KeyValuePair<string, object> pair)
        {
            // make sure key exist
            if (pair.Key == null)
            {
                return false;
            }

            if (pair.Value == null)
            {
                return true;
            }

            var type = pair.Value.GetType();
            return supportedTypes.Contains(type);
        }

        public IEnumerable<KeyValuePair<string, object>> Deserialize(string encodedString)
        {
            var xmlOptions = XElement.Parse(encodedString);
            if (!OptionsString.Equals(xmlOptions.Name.LocalName))
            {
                return SpecializedCollections.EmptyEnumerable<KeyValuePair<string, object>>();
            }

            var pairs = from option in xmlOptions.Elements(OptionString)
                        let pair = GetKeyValuePair(option)
                        where pair.HasValue
                        select pair.Value;

            return pairs;
        }

        private KeyValuePair<string, object>? GetKeyValuePair(XElement option)
        {
            //// TODO : how to report error?
            var nameAttribute = option.Attribute(NameString);
            var typeAttribute = option.Attribute(TypeString);
            var valueAttribute = option.Attribute(ValueString);

            if (nameAttribute == null || typeAttribute == null || valueAttribute == null)
            {
                return null;
            }

            var nameString = nameAttribute.Value;
            var typeString = typeAttribute.Value;
            var valueString = valueAttribute.Value;

            if (typeString == NullString)
            {
                return new KeyValuePair<string, object>(nameString, null);
            }

            try
            {
                Type type;
                if (stringToType.TryGetValue(typeString, out type))
                {
                    var value = DeserializeValue(valueString, type);
                    return new KeyValuePair<string, object>(nameString, value);
                }
            }
            catch (TargetInvocationException)
            {
                // all exceptions thrown by parsers will be of this type because the Parse(string)
                // method was found using reflection
            }

            return null;
        }

        public static object DeserializeValue(string valueString, Type type)
        {
            var converter = typeToConverter.GetOrAdd(type, getConverter);
            var value = converter(valueString);
            return value;
        }

        private static readonly Func<Type, Func<string, object>> getConverter = GetConverter;

        private static Func<string, object> GetConverter(Type type)
        {
            if (type == typeof(string))
            {
                return v => v;
            }

            var parseFunction = type.GetMethod("Parse", new[] { typeof(string) });
            return v => parseFunction.Invoke(null, new object[] { v });
        }
    }
}
