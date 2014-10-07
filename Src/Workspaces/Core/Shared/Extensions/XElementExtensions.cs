using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class XElementExtensions
    {
        public static XElement SingleElementOrDefault(this XElement element)
        {
            return element.Elements().SingleOrDefault();
        }

        public static void WriteTo(this XElement element, Stream stream)
        {
            using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
            {
                element.WriteTo(writer);
            }
        }
    }
}