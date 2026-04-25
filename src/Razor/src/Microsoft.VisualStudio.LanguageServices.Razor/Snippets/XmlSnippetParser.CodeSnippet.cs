// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Razor.Snippets;

internal partial class XmlSnippetParser
{
    /// <summary>
    /// Shamelessly copied from the editor
    /// https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Editor/VisualStudio/Impl/Snippet/CodeSnippet.cs
    /// </summary>
    internal class CodeSnippet
    {
        private const string ExpansionSnippetType = "Expansion";

        private readonly string[]? _snippetTypes;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="codeSnippetElement">XElement representing the CodeSnippet node.</param>
        public CodeSnippet(XElement codeSnippetElement)
        {
            var header = GetElementWithoutNamespace(codeSnippetElement, "Header");
            if (header == null)
            {
                throw new InvalidOperationException("snippet element is missing header.");
            }

            CodeSnippetElement = codeSnippetElement;

            Title = GetElementInnerText(header, "Title");
            Shortcut = GetElementInnerText(header, "Shortcut");
            var snippetTypes = GetElementsWithoutNamespace(header, "SnippetTypes");
            if (snippetTypes != null)
            {
                _snippetTypes = snippetTypes.Elements().Select(e => e.Value.Trim()).ToArray();
            }
        }

        public string Title { get; }

        public string Shortcut { get; }

        public XElement CodeSnippetElement { get; }

        public bool IsExpansionSnippet()
        {
            return _snippetTypes?.Contains(ExpansionSnippetType, StringComparer.OrdinalIgnoreCase) == true;
        }

        public static CodeSnippet ReadSnippetFromFile(string filePath, string snippetTitle)
        {
            var document = XDocument.Load(filePath);
            var snippets = ReadSnippets(document);

            var matchingSnippet = snippets.Single(s => string.Equals(s.Title, snippetTitle, StringComparison.OrdinalIgnoreCase));
            return matchingSnippet;
        }

        private static ImmutableArray<XElement> ReadCodeSnippetElements(XDocument document)
        {
            var codeSnippetsElement = document.Root;
            if (codeSnippetsElement is null)
                return ImmutableArray<XElement>.Empty;

            if (codeSnippetsElement.Name.LocalName.Equals("CodeSnippets", StringComparison.OrdinalIgnoreCase))
            {
                return codeSnippetsElement.Elements().Where(e => e.Name.LocalName.Equals("CodeSnippet", StringComparison.OrdinalIgnoreCase)).ToImmutableArray();
            }
            else if (codeSnippetsElement.Name.LocalName.Equals("CodeSnippet", StringComparison.OrdinalIgnoreCase))
            {
                return ImmutableArray.Create(codeSnippetsElement);
            }

            return ImmutableArray<XElement>.Empty;
        }

        public static IEnumerable<XElement> GetElementsWithoutNamespace(XElement element, string localName)
        {
            return element.Elements().Where(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
        }

        public static XElement? GetElementWithoutNamespace(XElement? element, string localName)
        {
            return element?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetElementInnerText(XElement element, string subElementName)
        {
            var subElement = GetElementWithoutNamespace(element, subElementName);
            return subElement == null ? string.Empty : subElement.Value.Trim();
        }

        public static ImmutableArray<CodeSnippet> ReadSnippets(XDocument document)
        {
            return ReadCodeSnippetElements(document).SelectAsArray(element => new CodeSnippet(element));
        }
    }
}
