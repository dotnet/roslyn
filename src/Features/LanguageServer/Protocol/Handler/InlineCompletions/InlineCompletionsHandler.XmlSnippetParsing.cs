// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

internal partial class InlineCompletionsHandler
{
    /// <summary>
    /// Shamelessly copied from the editor
    /// https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Editor/VisualStudio/Impl/Snippet/CodeSnippet.cs
    /// 
    /// When we switch LSP over to semantic snippets we should remove this.
    /// </summary>
    private class CodeSnippet
    {
        private readonly string[]? _snippetTypes;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="codeSnippetElement">XElement representing the CodeSnippet node.</param>
        /// <param name="filePath">File path from where the code snippet is loaded.  Can be null if not loaded from file.</param>
        /// <param name="xml">The full XML of the snippet.</param>
        public CodeSnippet(XElement codeSnippetElement, string? filePath, string? xml)
        {
            if (codeSnippetElement == null)
            {
                throw new ArgumentNullException(nameof(codeSnippetElement));
            }

            if (string.IsNullOrEmpty(filePath) && string.IsNullOrEmpty(xml))
            {
                throw new ArgumentException("filePath and xml cannot be both empty");
            }

            FilePath = filePath;
            Xml = xml;

            var header = GetElementWithoutNamespace(codeSnippetElement, "Header");
            if (header == null)
            {
                throw new InvalidOperationException("snippet element is missing header.");
            }

            Title = GetElementInnerText(header, "Title");
            Shortcut = GetElementInnerText(header, "Shortcut");
            Description = GetElementInnerText(header, "Description");
            var snippetTypes = GetElementsWithoutNamespace(header, "SnippetTypes");
            if (snippetTypes != null)
            {
                _snippetTypes = snippetTypes.Elements().Select(e => e.Value.Trim()).ToArray();
            }

            var snippetElement = GetElementWithoutNamespace(codeSnippetElement, "Snippet");
            if (snippetElement != null)
            {
                var code = GetElementWithoutNamespace(snippetElement, "Code");
                if (code != null)
                {
                    var kind = code.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals("Kind", StringComparison.OrdinalIgnoreCase));
                    if (kind != null)
                    {
                        SnippetKind = kind.Value;
                    }
                }
            }
        }

        public string Title
        {
            get;
        }

        public string Shortcut
        {
            get;
        }

        public string Description
        {
            get;
        }

        public IEnumerable<string>? SnippetTypes
        {
            get { return _snippetTypes; }
        }

        public string? SnippetKind
        {
            get;
        }

        public string? Xml
        {
            get;
        }

        public string? FilePath
        {
            get;
        }

        public static IEnumerable<CodeSnippet> ReadSnippetsFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                var document = XDocument.Load(filePath);
                return ReadSnippets(document, filePath, xml: null);
            }
            catch (XmlException)
            {
                return SpecializedCollections.EmptyEnumerable<CodeSnippet>();
            }
        }

        public static IEnumerable<CodeSnippet> ReadSnippetsFromText(string text)
        {
            try
            {
                var document = XDocument.Parse(text);
                return ReadSnippets(document: document, filePath: null, xml: text);
            }
            catch (XmlException)
            {
                return SpecializedCollections.EmptyEnumerable<CodeSnippet>();
            }
        }

        public static IEnumerable<XElement>? ReadCodeSnippetElements(XDocument document)
        {
            try
            {
                var codeSnippetsElement = document.Root;
                IEnumerable<XElement>? codeSnippetElements = null;
                if (codeSnippetsElement.Name.LocalName.Equals("CodeSnippets", StringComparison.OrdinalIgnoreCase))
                {
                    codeSnippetElements = codeSnippetsElement.Elements().Where(e => e.Name.LocalName.Equals("CodeSnippet", StringComparison.OrdinalIgnoreCase));
                }
                else if (codeSnippetsElement.Name.LocalName.Equals("CodeSnippet", StringComparison.OrdinalIgnoreCase))
                {
                    codeSnippetElements = new[] { codeSnippetsElement };
                }

                return codeSnippetElements;
            }
            catch (XmlException)
            {
                return SpecializedCollections.EmptyEnumerable<XElement>();
            }
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

        private static IEnumerable<CodeSnippet> ReadSnippets(XDocument document, string? filePath, string? xml)
        {
            return ReadCodeSnippetElements(document).Select(element => new CodeSnippet(element, filePath, xml));
        }
    }

    /// <summary>
    /// Shamelessly stolen from https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Editor/VisualStudio/Impl/Snippet/ExpansionTemplate.cs
    /// with minor changes to parsing to support LSP snippets.
    /// </summary>
    private class ExpansionTemplate
    {
        private const string _selected = "selected";
        private const string _end = "end";

        private string? _snippetFullText;
        private readonly List<ExpansionField> _tokens = new List<ExpansionField>();
        private int? _endOffset;

        private XElement? _snippetElement;
        private XElement? _declarationsElement;
        private string? _code;
        private string _delimiter = "$";

        public readonly CodeSnippet Snippet;

        public ExpansionTemplate(CodeSnippet snippet)
        {
            LoadTemplate(snippet);
            Parse();

            this.Snippet = snippet;
        }

        public string? GetCodeSnippet()
        {
            return _snippetFullText;
        }

        public int? GetEndOffset()
        {
            return _endOffset;
        }

        internal IEnumerable<ExpansionField> Fields
        {
            get
            {
                return _tokens;
            }
        }

        private void LoadTemplate(CodeSnippet snippet)
        {
            XDocument? document = null;
            if (File.Exists(snippet.FilePath))
            {
                document = XDocument.Load(snippet.FilePath);
            }
            else if (!string.IsNullOrEmpty(snippet.Xml))
            {
                document = XDocument.Parse(snippet.Xml);
            }

            if (document == null)
            {
                throw new InvalidOperationException("snippet document is null.");
            }

            var codeSnippetElements = CodeSnippet.ReadCodeSnippetElements(document);
            if (codeSnippetElements == null)
            {
                throw new InvalidOperationException("document does not contain code snippet elements.");
            }

            XElement? matchedCodeSnippet = null;
            foreach (var element in codeSnippetElements)
            {
                var header = CodeSnippet.GetElementWithoutNamespace(element, "Header");
                var title = CodeSnippet.GetElementWithoutNamespace(header, "Title");
                if (string.Equals(snippet.Title, title?.Value?.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedCodeSnippet = element;
                    break;
                }
            }

            if (matchedCodeSnippet != null)
            {
                _snippetElement = CodeSnippet.GetElementWithoutNamespace(matchedCodeSnippet, "Snippet");
                _declarationsElement = CodeSnippet.GetElementWithoutNamespace(_snippetElement, "Declarations");
                ReadDeclarations(_declarationsElement);
                var code = CodeSnippet.GetElementWithoutNamespace(_snippetElement, "Code");
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
        }

        private void ReadDeclarations(XElement? declarations)
        {
            if (declarations == null)
            {
                return;
            }

            foreach (var declarationElement in declarations.Elements())
            {
                var defaultAttribute = declarationElement.Attribute("Default");
                var editableAttribute = declarationElement.Attribute("Editable");
                _tokens.Add(new ExpansionField(
                    CodeSnippet.GetElementInnerText(declarationElement, "ID"),
                    CodeSnippet.GetElementInnerText(declarationElement, "Default") ?? " ",
                    CodeSnippet.GetElementWithoutNamespace(declarationElement, "Function"),
                    defaultAttribute != null && string.Equals(defaultAttribute.Value, "true", StringComparison.Ordinal),
                    editableAttribute == null || string.Equals(editableAttribute.Value, "true", StringComparison.Ordinal) || string.Equals(editableAttribute.Value, "1", StringComparison.Ordinal)));
            }
        }

        private void Parse()
        {
            var pooledStringBuilder = PooledStringBuilder.GetInstance();
            var total = pooledStringBuilder.Builder;
            int iTokenLen;
            var currentCharIndex = 0;
            var currentTokenCharIndex = 0;
            var sps = SnippetParseState.Code;

            // LSP tab stop locations start at 1.
            var nextAvailableTabStopIndex = 1;

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
                                total.Append(_code.Substring(currentTokenCharIndex, iTokenLen));
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
                                if (string.Equals(fieldName, _selected, StringComparison.Ordinal))
                                {
                                    // LSP client currently only invokes on typing (tab) so there is no way to have a selection as part of a snippet request.
                                    // Additionally, TM_SELECTED_TEXT is not supported in the VS LSP client, so we can't set the selection even if we wanted to.
                                    // Since there's no way for the user to ask for a selection replacement, we can ignore it.
                                }
                                else if (string.Equals(fieldName, _end, StringComparison.Ordinal))
                                {
                                    var endToken = "/*$0*/";
                                    _endOffset = total.Length;

                                    // LSP indicates the final cursor location with $0.
                                    // Add in a multi-line comment token that we can attach an annotation for formatting.
                                    total.Append(endToken);
                                }
                                else
                                {
                                    var field = FindField(fieldName);
                                    if (field != null)
                                    {
                                        field.AddOffset(total.Length);
                                        total.Append(field.Default);

                                        // Set the tab stop index for the field in the order we see them in the code.
                                        field.GetOrSetTabStopIndexForField(ref nextAvailableTabStopIndex);
                                    }
                                }
                            }
                            else
                            {
                                // simply append a '$'    
                                total.Append(_delimiter);
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
                total.Append(_code.Substring(currentTokenCharIndex, currentCharIndex - currentTokenCharIndex));
            }

            _snippetFullText = pooledStringBuilder.ToStringAndFree();
        }

        private ExpansionField FindField(string fieldName)
        {
            return _tokens.FirstOrDefault(t => string.Equals(t.ID, fieldName, StringComparison.Ordinal));
        }

        private enum SnippetParseState
        {
            Code,
            Literal
        }
    }

    internal class ExpansionField
    {
        private int? _fieldTabStopIndex;
        private readonly List<int> _offsets = new List<int>();

        public ExpansionField(string id, string @default, XElement? function, bool isDefault, bool isEditable)
        {
            ID = id ?? throw new ArgumentNullException(nameof(id));
            Default = @default ?? throw new ArgumentNullException(nameof(@default));
            Function = function;
            IsDefault = isDefault;
            IsEditable = isEditable;

            _fieldTabStopIndex = null;
        }

        public string ID { get; }

        public string Default
        {
            get;
        }

        public XElement? Function
        {
            get;
        }

        public bool IsDefault
        {
            get;
        }

        public bool IsEditable
        {
            get;
        }

        public ImmutableArray<int> GetOffsets()
        {
            return _offsets.ToImmutableArray();
        }

        public void AddOffset(int offset)
        {
            _offsets.Add(offset);
        }

        /// <summary>
        /// Gets the tab stop index associated with this field.
        /// If none, associate this field with the next available tab stop index and
        /// set increment the next available index (as the prev value is now associated with this field).
        /// </summary>
        public int GetOrSetTabStopIndexForField(ref int nextAvailableFieldTabStopIndex)
        {
            if (_fieldTabStopIndex == null)
            {
                _fieldTabStopIndex = nextAvailableFieldTabStopIndex;
                nextAvailableFieldTabStopIndex++;
            }

            return _fieldTabStopIndex.Value;
        }

        public int GetTabStopIndex()
        {
            Contract.ThrowIfNull(_fieldTabStopIndex);
            return _fieldTabStopIndex.Value;
        }
    }
}
