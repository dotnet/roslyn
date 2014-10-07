using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class XmlDocumentationCommentUtilities
    {
        public static IEnumerable<SymbolDisplayPart> IntersperseWithSpaces(this IEnumerable<IEnumerable<SymbolDisplayPart>> items)
        {
            var separator = new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");

            var arr = items.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                foreach (var actualItem in arr[i])
                {
                    yield return actualItem;
                }

                if (i < arr.Length - 1)
                {
                    // Add a separator if we're not next to a new line.
                    if (arr[i + 1].First().Kind != SymbolDisplayPartKind.LineBreak && arr[i].First().Kind != SymbolDisplayPartKind.LineBreak)
                    {
                        yield return separator;
                    }
                }
            }
        }

        public static readonly SymbolDisplayFormat CrefFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static XmlReader CreateFragmentReader(string xml)
        {
            return XmlReader.Create(
                input: new StringReader(xml),
                settings: new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
        }

        public static List<SymbolDisplayPart> TryGetXmlDocumentation(string documentation, SemanticModel semanticModel, int position)
        {
            using (var reader = CreateFragmentReader(documentation))
            {
                var parts = new List<IEnumerable<SymbolDisplayPart>>();
                while (reader.Read())
                {
                    if (reader.LocalName == "")
                    {
                        var text = reader.Value;
                        var lines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        var usableLines = lines.Select(l => l.Trim()).Where(l => l.Length > 0);

                        parts.AddRange(usableLines.Select(l => SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, l.Trim()))));
                    }

                    if (reader.LocalName == "para" && reader.NodeType == XmlNodeType.Element)
                    {
                        parts.Add(SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n")));
                    }

                    if (reader.LocalName == "see" || reader.LocalName == "seealso")
                    {
                        var cref = reader.GetAttribute("cref");

                        if (string.IsNullOrEmpty(cref))
                        {
                            continue;
                        }

                        var docSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, semanticModel.Compilation);
                        if (docSymbol != null)
                        {
                            parts.Add(docSymbol.ToMinimalDisplayParts(semanticModel, position, CrefFormat));
                        }
                        else
                        {
                            var splitCref = cref.Split(':');

                            if (splitCref.Length > 1)
                            {
                                parts.Add(SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, splitCref[1])));
                            }
                            else
                            {
                                parts.Add(SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, cref)));
                            }
                        }
                    }
                }

                return parts.IntersperseWithSpaces().ToList();
            }
        }
    }
}
