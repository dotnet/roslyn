// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Razor.Snippets;

internal partial class XmlSnippetParser
{
    /// <summary>
    /// Shamelessly adapted from https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Editor/VisualStudio/Impl/Snippet/ExpansionTemplate.cs
    /// with changes to parsing to store the snippet as a set of parts instead of a single string.
    /// </summary>
    private class ExpansionTemplate
    {
        private record ExpansionField(string ID, string Default, string? FunctionName, string? FunctionParam, bool IsEditable);

        private const string Selected = "selected";
        private const string End = "end";
        private const string Shortcut = "shortcut";

        private readonly List<ExpansionField> _tokens = new();

        private readonly string? _code;
        private readonly char _delimiter = '$';
        private readonly SnippetStringPart _delimiterPart;

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
            if (delimiterAttribute?.Value is string { Length: 1 } && !string.IsNullOrWhiteSpace(delimiterAttribute.Value))
            {
                _delimiter = delimiterAttribute.Value[0];
            }

            _delimiterPart = new(_delimiter.ToString());
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
                TryGetSnippetFunctionInfo(functionElement?.Value, out var functionName, out var functionParam);
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
            var parserState = SnippetParseState.Code;

            // Associate the field id to the index of the field in the snippet.
            using var fieldNameToSnippetIndex = new PooledDictionaryBuilder<string, int>();
            var currentTabStopIndex = 1;

            using var snippetParts = new PooledArrayBuilder<SnippetPart>();

            // Mechanically ported from env/msenv/textmgr/ExpansionTemplate.cpp
            while (currentCharIndex < _code!.Length)
            {
                iTokenLen = currentCharIndex - currentTokenCharIndex;

                switch (parserState)
                {
                    case SnippetParseState.Code:
                        if (_code[currentCharIndex] == _delimiter)
                        {
                            // we just hit a $, denoting a literal
                            parserState = SnippetParseState.Literal;

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
                        if (_code[currentCharIndex] == _delimiter)
                        {
                            // we just hit the $, ending the literal
                            parserState = SnippetParseState.Code;

                            // if we have any token, it's a literal, otherwise it's an escaped '$'
                            if (iTokenLen > 0)
                            {
                                var fieldName = _code[currentTokenCharIndex..currentCharIndex];

                                // first check to see if this is a "special" literal
                                if (string.Equals(fieldName, Selected, StringComparison.Ordinal))
                                {
                                    // LSP client currently only invokes on typing (tab) so there is no way to have a selection as part of a snippet request.
                                    // Additionally, TM_SELECTED_TEXT is not supported in the VS LSP client, so we can't set the selection even if we wanted to.
                                    // Since there's no way for the user to ask for a selection replacement, we can ignore it.
                                }
                                else if (string.Equals(fieldName, End, StringComparison.Ordinal))
                                {
                                    snippetParts.Add(SnippetCursorPart.Instance);
                                }
                                else if (string.Equals(fieldName, Shortcut, StringComparison.Ordinal))
                                {
                                    snippetParts.Add(SnippetShortcutPart.Instance);
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
                                            : new SnippetFunctionPart(field.ID, field.Default, fieldIndex, field.FunctionName!, field.FunctionParam);
                                        snippetParts.Add(fieldPart);
                                    }
                                }
                            }
                            else
                            {
                                // simply append a '$'    
                                snippetParts.Add(_delimiterPart);
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
            if (parserState == SnippetParseState.Code && (currentCharIndex > currentTokenCharIndex))
            {
                var remaining = _code[currentTokenCharIndex..currentCharIndex];
                snippetParts.Add(new SnippetStringPart(remaining));
            }

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

        /// <summary>
        /// Parse the XML snippet function attribute to determine the function name and parameter.
        /// </summary>
        private static bool TryGetSnippetFunctionInfo(
            string? xmlFunctionText,
            [NotNullWhen(true)] out string? snippetFunctionName,
            [NotNullWhen(true)] out string? param)
        {
            if (string.IsNullOrEmpty(xmlFunctionText))
            {
                snippetFunctionName = null;
                param = null;
                return false;
            }

            xmlFunctionText.AssumeNotNull();

            if (!xmlFunctionText.Contains('(') ||
                !xmlFunctionText.Contains(')') ||
                xmlFunctionText.IndexOf(')') < xmlFunctionText.IndexOf('('))
            {
                snippetFunctionName = null;
                param = null;
                return false;
            }

            snippetFunctionName = xmlFunctionText[..xmlFunctionText.IndexOf('(')];

            var paramStart = xmlFunctionText.IndexOf('(') + 1;
            var paramLength = xmlFunctionText.LastIndexOf(')') - xmlFunctionText.IndexOf('(') - 1;
            param = xmlFunctionText.Substring(paramStart, paramLength);
            return true;
        }
    }
}
