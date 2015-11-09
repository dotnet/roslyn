// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationCommentFormatting
{
    internal abstract class AbstractDocumentationCommentFormattingService : IDocumentationCommentFormattingService
    {
        private int _position;
        private SemanticModel _semanticModel;

        private class FormatterState
        {
            private bool _anyNonWhitespaceSinceLastPara;
            private bool _pendingParagraphBreak;
            private bool _pendingSingleSpace;

            private static SymbolDisplayPart s_spacePart = new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
            private static SymbolDisplayPart s_newlinePart = new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");

            internal readonly List<SymbolDisplayPart> Builder = new List<SymbolDisplayPart>();

            internal SemanticModel SemanticModel { get; set; }
            internal int Position { get; set; }

            public bool AtBeginning
            {
                get
                {
                    return Builder.Count == 0;
                }
            }

            public SymbolDisplayFormat Format { get; internal set; }

            public void AppendSingleSpace()
            {
                _pendingSingleSpace = true;
            }

            public void AppendString(string s)
            {
                EmitPendingChars();

                Builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, s));

                _anyNonWhitespaceSinceLastPara = true;
            }

            public void AppendParts(IEnumerable<SymbolDisplayPart> parts)
            {
                EmitPendingChars();

                Builder.AddRange(parts);

                _anyNonWhitespaceSinceLastPara = true;
            }

            public void MarkBeginOrEndPara()
            {
                // If this is a <para> with nothing before it, then skip it.
                if (_anyNonWhitespaceSinceLastPara == false)
                {
                    return;
                }

                _pendingParagraphBreak = true;

                // Reset flag.
                _anyNonWhitespaceSinceLastPara = false;
            }

            public string GetText()
            {
                return Builder.GetFullText();
            }

            private void EmitPendingChars()
            {
                if (_pendingParagraphBreak)
                {
                    Builder.Add(s_newlinePart);
                    Builder.Add(s_newlinePart);
                }
                else if (_pendingSingleSpace)
                {
                    Builder.Add(s_spacePart);
                }

                _pendingParagraphBreak = false;
                _pendingSingleSpace = false;
            }
        }

        public string Format(string rawXmlText, Compilation compilation = null)
        {
            if (rawXmlText == null)
            {
                return null;
            }

            var state = new FormatterState();

            // In case the XML is a fragment (that is, a series of elements without a parent)
            // wrap it up in a single tag. This makes parsing it much, much easier.
            var inputString = "<tag>" + rawXmlText + "</tag>";

            var summaryElement = XElement.Parse(inputString, LoadOptions.PreserveWhitespace);

            AppendTextFromNode(state, summaryElement, compilation);

            return state.GetText();
        }

        public IEnumerable<SymbolDisplayPart> Format(string rawXmlText, SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
        {
            if (rawXmlText == null)
            {
                return null;
            }

            var state = new FormatterState() { SemanticModel = semanticModel, Position = position, Format = format };

            // In case the XML is a fragment (that is, a series of elements without a parent)
            // wrap it up in a single tag. This makes parsing it much, much easier.
            var inputString = "<tag>" + rawXmlText + "</tag>";

            var summaryElement = XElement.Parse(inputString, LoadOptions.PreserveWhitespace);

            AppendTextFromNode(state, summaryElement, state.SemanticModel.Compilation);

            return state.Builder;
        }

        private static void AppendTextFromNode(FormatterState state, XNode node, Compilation compilation)
        {
            if (node.NodeType == XmlNodeType.Text)
            {
                AppendTextFromTextNode(state, (XText)node);
            }

            if (node.NodeType != XmlNodeType.Element)
            {
                return;
            }

            var element = (XElement)node;

            var name = element.Name.LocalName;

            if (name == "see" ||
                name == "seealso")
            {
                foreach (var attribute in element.Attributes())
                {
                    AppendTextFromAttribute(state, element, attribute, attributeNameToParse: "cref");
                }

                return;
            }
            else if (name == "paramref" ||
                     name == "typeparamref")
            {
                foreach (var attribute in element.Attributes())
                {
                    AppendTextFromAttribute(state, element, attribute, attributeNameToParse: "name");
                }

                return;
            }

            if (name == "para")
            {
                state.MarkBeginOrEndPara();
            }

            foreach (var childNode in element.Nodes())
            {
                AppendTextFromNode(state, childNode, compilation);
            }

            if (name == "para")
            {
                state.MarkBeginOrEndPara();
            }
        }

        private static void AppendTextFromAttribute(FormatterState state, XElement element, XAttribute attribute, string attributeNameToParse)
        {
            var attributeName = attribute.Name.LocalName;
            if (attributeNameToParse == attributeName)
            {
                state.AppendParts(CrefToSymbolDisplayParts(attribute.Value, state.Position, state.SemanticModel, state.Format));
            }
            else
            {
                var displayKind = attributeName == "langword"
                    ? SymbolDisplayPartKind.Keyword
                    : SymbolDisplayPartKind.Text;
                state.AppendParts(SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(kind: displayKind, symbol: null, text: attribute.Value)));
            }
        }

        internal static IEnumerable<SymbolDisplayPart> CrefToSymbolDisplayParts(string crefValue, int position, SemanticModel semanticModel, SymbolDisplayFormat format = null)
        {
            // first try to parse the symbol
            if (semanticModel != null)
            {
                var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefValue, semanticModel.Compilation);
                if (symbol != null)
                {
                    if (symbol.IsConstructor())
                    {
                        format = format.WithMemberOptions(SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface);
                    }

                    return symbol.ToMinimalDisplayParts(semanticModel, position, format);
                }
            }

            // if any of that fails fall back to just displaying the raw text
            return SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(kind: SymbolDisplayPartKind.Text, symbol: null, text: TrimCrefPrefix(crefValue)));
        }

        private static string TrimCrefPrefix(string value)
        {
            if (value.Length >= 2 && value[1] == ':')
            {
                value = value.Substring(startIndex: 2);
            }

            return value;
        }

        private static void AppendTextFromTextNode(FormatterState state, XText element)
        {
            var rawText = element.Value;
            var builder = new StringBuilder(rawText.Length);

            // Normalize the whitespace.
            var pendingWhitespace = false;
            var hadAnyNonWhitespace = false;
            for (int i = 0; i < rawText.Length; i++)
            {
                if (char.IsWhiteSpace(rawText[i]))
                {
                    // Whitespace. If it occurs at the beginning of the text we don't append it
                    // at all; otherwise, we reduce it to a single space.
                    if (!state.AtBeginning || hadAnyNonWhitespace)
                    {
                        pendingWhitespace = true;
                    }
                }
                else
                {
                    // Some other character...
                    if (pendingWhitespace)
                    {
                        if (builder.Length == 0)
                        {
                            state.AppendSingleSpace();
                        }
                        else
                        {
                            builder.Append(' ');
                        }

                        pendingWhitespace = false;
                    }

                    builder.Append(rawText[i]);
                    hadAnyNonWhitespace = true;
                }
            }

            if (builder.Length > 0)
            {
                state.AppendString(builder.ToString());
            }

            if (pendingWhitespace)
            {
                state.AppendSingleSpace();
            }
        }
    }
}
