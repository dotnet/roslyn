// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

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
            Contract.ThrowIfNull(snippets, $"Did not find any code snippets in {filePath}");

            var matchingSnippet = snippets.Value.Single(s => string.Equals(s.Title, snippetTitle, StringComparison.OrdinalIgnoreCase));
            return matchingSnippet;
        }

        private static ImmutableArray<XElement>? ReadCodeSnippetElements(XDocument document)
        {
            var codeSnippetsElement = document.Root;
            if (codeSnippetsElement is null)
                return null;

            if (codeSnippetsElement.Name.LocalName.Equals("CodeSnippets", StringComparison.OrdinalIgnoreCase))
            {
                return codeSnippetsElement.Elements().Where(e => e.Name.LocalName.Equals("CodeSnippet", StringComparison.OrdinalIgnoreCase)).ToImmutableArray();
            }
            else if (codeSnippetsElement.Name.LocalName.Equals("CodeSnippet", StringComparison.OrdinalIgnoreCase))
            {
                return ImmutableArray.Create(codeSnippetsElement);
            }

            return null;
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

        /// <summary>
        /// Visible for testing.
        /// </summary>
        internal static ImmutableArray<CodeSnippet>? ReadSnippets(XDocument document)
        {
            return ReadCodeSnippetElements(document)?.Select(element => new CodeSnippet(element)).ToImmutableArray();
        }
    }

    /// <summary>
    /// Shamelessly adapted from https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Editor/VisualStudio/Impl/Snippet/ExpansionTemplate.cs
    /// with changes to parsing to store the snippet as a set of parts instead of a single string.
    /// </summary>
    private class ExpansionTemplate
    {
        private record ExpansionField(string ID, string Default, string? FunctionName, string? FunctionParam, bool IsEditable);

        private const string Selected = "selected";
        private const string End = "end";

        private readonly List<ExpansionField> _tokens = new();

        private readonly string? _code;
        private readonly string _delimiter = "$";

        public ExpansionTemplate(CodeSnippet snippet)
        {
            var snippetElement = CodeSnippet.GetElementWithoutNamespace(snippet.CodeSnippetElement, "Snippet");
            var declarationsElement = CodeSnippet.GetElementWithoutNamespace(snippetElement, "Declarations");
            ReadDeclarations(declarationsElement);
            var code = CodeSnippet.GetElementWithoutNamespace(snippetElement, "Code");
            if (code == null)
            {
                throw new InvalidOperationException("snippet is missing code element.");
            }

            _code = Regex.Replace(code.Value, "(?<!\r)\n", "\r\n");
            var delimiterAttribute = code.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals("Delimiter", StringComparison.OrdinalIgnoreCase));
            if (delimiterAttribute != null)
            {
                _delimiter = delimiterAttribute.Value;
            }
        }

        private void ReadDeclarations(XElement? declarations)
        {
            if (declarations == null)
            {
                return;
            }

            foreach (var declarationElement in declarations.Elements())
            {
                var editableAttribute = declarationElement.Attribute("Editable");
                var functionElement = CodeSnippet.GetElementWithoutNamespace(declarationElement, "Function");
                SnippetFunctionService.TryGetSnippetFunctionInfo(functionElement?.Value, out var functionName, out var functionParam);
                _tokens.Add(new ExpansionField(
                    CodeSnippet.GetElementInnerText(declarationElement, "ID"),
                    CodeSnippet.GetElementInnerText(declarationElement, "Default") ?? " ",
                    functionName,
                    functionParam,
                    editableAttribute == null || string.Equals(editableAttribute.Value, "true", StringComparison.Ordinal) || string.Equals(editableAttribute.Value, "1", StringComparison.Ordinal)));
            }
        }

        internal ParsedXmlSnippet Parse()
        {
            int iTokenLen;
            var currentCharIndex = 0;
            var currentTokenCharIndex = 0;
            var sps = SnippetParseState.Code;

            // Associate the field id to the index of the field in the snippet.
            var fieldNameToSnippetIndex = new Dictionary<string, int>();
            var currentTabStopIndex = 1;

            using var builder = ArrayBuilder<SnippetPart>.GetInstance(out var snippetParts);

            // Mechanically ported from env/msenv/textmgr/ExpansionTemplate.cpp
            while (currentCharIndex < _code!.Length)
            {
                iTokenLen = currentCharIndex - currentTokenCharIndex;

                switch (sps)
                {
                    case SnippetParseState.Code:
                        if (string.Equals(_code[currentCharIndex].ToString(CultureInfo.CurrentCulture), _delimiter, StringComparison.Ordinal))
                        {
                            // we just hit a $, denoting a literal
                            sps = SnippetParseState.Literal;

                            // copy anything from the previous token into our string
                            if (currentCharIndex > currentTokenCharIndex)
                            {
                                // append the token into our buffer
                                var token = _code.Substring(currentTokenCharIndex, iTokenLen);
                                snippetParts.Add(new SnippetStringPart(token));
                            }

                            // start the new token at the next character
                            currentTokenCharIndex = currentCharIndex;
                            currentTokenCharIndex++;
                        }

                        break;

                    case SnippetParseState.Literal:
                        if (string.Equals(_code[currentCharIndex].ToString(CultureInfo.CurrentCulture), _delimiter, StringComparison.Ordinal))
                        {
                            // we just hit the $, ending the literal
                            sps = SnippetParseState.Code;

                            // if we have any token, it's a literal, otherwise it's an escaped '$'
                            if (iTokenLen > 0)
                            {
                                // allocate a buffer and get the string name of this literal
                                var fieldNameLength = currentCharIndex - currentTokenCharIndex;

                                var fieldName = _code.Substring(currentTokenCharIndex, fieldNameLength);

                                // first check to see if this is a "special" literal
                                if (string.Equals(fieldName, Selected, StringComparison.Ordinal))
                                {
                                    // LSP client currently only invokes on typing (tab) so there is no way to have a selection as part of a snippet request.
                                    // Additionally, TM_SELECTED_TEXT is not supported in the VS LSP client, so we can't set the selection even if we wanted to.
                                    // Since there's no way for the user to ask for a selection replacement, we can ignore it.
                                }
                                else if (string.Equals(fieldName, End, StringComparison.Ordinal))
                                {
                                    snippetParts.Add(new SnippetCursorPart());
                                }
                                else
                                {
                                    var field = FindField(fieldName);
                                    if (field != null)
                                    {
                                        // If we have an editable field we need to know its order in the snippet so we can place the appropriate tab stop indices.
                                        int? fieldIndex = field.IsEditable ? fieldNameToSnippetIndex.GetOrAdd(field.ID, (key) => currentTabStopIndex++) : null;
                                        var fieldPart = string.IsNullOrEmpty(field.FunctionName)
                                                    ? new SnippetFieldPart(field.ID, field.Default, fieldIndex)
                                                    : new SnippetFunctionPart(field.ID, field.Default, fieldIndex, field.FunctionName, field.FunctionParam);
                                        snippetParts.Add(fieldPart);
                                    }
                                }
                            }
                            else
                            {
                                // simply append a '$'    
                                snippetParts.Add(new SnippetStringPart(_delimiter));
                            }

                            // start the new token at the next character
                            currentTokenCharIndex = currentCharIndex;
                            currentTokenCharIndex++;
                        }

                        break;
                }

                currentCharIndex++;
            }

            // do we have any remaining text to be copied?
            if (sps == SnippetParseState.Code && (currentCharIndex > currentTokenCharIndex))
            {
                var remaining = _code[currentTokenCharIndex..currentCharIndex];
                snippetParts.Add(new SnippetStringPart(remaining));
            }

            Contract.ThrowIfFalse(snippetParts.Any());

            return new ParsedXmlSnippet(snippetParts.ToImmutable());
        }

        private ExpansionField? FindField(string fieldName)
        {
            return _tokens.FirstOrDefault(t => string.Equals(t.ID, fieldName, StringComparison.Ordinal));
        }

        private enum SnippetParseState
        {
            Code,
            Literal
        }
    }
}
