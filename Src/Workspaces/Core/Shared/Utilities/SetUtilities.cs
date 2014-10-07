using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class SetUtilities
    {
        private const string SerializationFormatAttributeName = "SerializationFormat";
        private const string SerializationFormat = "0";

        private const string SetElementName = "Set";
        private const string ValueElementName = "V";

        public static XElement ToXElement<T>(this ISet<T> set, Func<T, object> convert)
        {
            return new XElement(SetElementName,
                new XAttribute(SerializationFormatAttributeName, SerializationFormat),
                set == null ? null : set.Select(v => new XElement(ValueElementName, convert(v))));
        }

        public static ISet<T> FromXElement<T>(XElement element, Func<XElement, T> convert)
        {
            if (element != null && element.Name == SetElementName)
            {
                if ((string)element.Attribute(SerializationFormatAttributeName) == SerializationFormat)
                {
                    return new HashSet<T>(
                        element.Elements(ValueElementName).Select(convert));
                }
            }

            return null;
        }
    }
}