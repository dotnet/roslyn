// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal abstract class AbstractDocumentationCommentFormattingService : IDocumentationCommentFormattingService
    {
        private class FormatterState
        {
            private bool _anyNonWhitespaceSinceLastPara;
            private bool _pendingParagraphBreak;
            private bool _pendingLineBreak;
            private bool _pendingSingleSpace;

            private static readonly TaggedText s_spacePart = new TaggedText(TextTags.Space, " ");
            private static readonly TaggedText s_newlinePart = new TaggedText(TextTags.LineBreak, "\r\n");

            internal readonly List<TaggedText> Builder = new List<TaggedText>();

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

                Builder.Add(new TaggedText(TextTags.Text, s));

                _anyNonWhitespaceSinceLastPara = true;
            }

            public void AppendParts(IEnumerable<TaggedText> parts)
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

            public void MarkLineBreak()
            {
                // If this is a <br> with nothing before it, then skip it.
                if (_anyNonWhitespaceSinceLastPara == false && !_pendingLineBreak)
                {
                    return;
                }

                if (_pendingLineBreak || _pendingParagraphBreak)
                {
                    // Multiple line breaks in sequence become a single paragraph break.
                    _pendingParagraphBreak = true;
                    _pendingLineBreak = false;
                }
                else
                {
                    _pendingLineBreak = true;
                }

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
                else if (_pendingLineBreak)
                {
                    Builder.Add(s_newlinePart);
                }
                else if (_pendingSingleSpace)
                {
                    Builder.Add(s_spacePart);
                }

                _pendingParagraphBreak = false;
                _pendingLineBreak = false;
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

        public IEnumerable<TaggedText> Format(string rawXmlText, SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
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

            if (name == DocumentationCommentXmlNames.SeeElementName ||
                name == DocumentationCommentXmlNames.SeeAlsoElementName)
            {
                if (element.IsEmpty || element.FirstNode == null)
                {
                    foreach (var attribute in element.Attributes())
                    {
                        AppendTextFromAttribute(state, element, attribute, attributeNameToParse: DocumentationCommentXmlNames.CrefAttributeName, SymbolDisplayPartKind.Text);
                    }

                    return;
                }
            }
            else if (name == DocumentationCommentXmlNames.ParameterReferenceElementName ||
                     name == DocumentationCommentXmlNames.TypeParameterReferenceElementName)
            {
                var kind = name == DocumentationCommentXmlNames.ParameterReferenceElementName ? SymbolDisplayPartKind.ParameterName : SymbolDisplayPartKind.TypeParameterName;
                foreach (var attribute in element.Attributes())
                {
                    AppendTextFromAttribute(state, element, attribute, attributeNameToParse: DocumentationCommentXmlNames.NameAttributeName, kind);
                }

                return;
            }

            if (name == DocumentationCommentXmlNames.ParaElementName)
            {
                state.MarkBeginOrEndPara();
            }
            else if (name == "br")
            {
                state.MarkLineBreak();
            }

            foreach (var childNode in element.Nodes())
            {
                AppendTextFromNode(state, childNode, compilation);
            }

            if (name == DocumentationCommentXmlNames.ParaElementName)
            {
                state.MarkBeginOrEndPara();
            }
        }

        private static void AppendTextFromAttribute(FormatterState state, XElement element, XAttribute attribute, string attributeNameToParse, SymbolDisplayPartKind kind)
        {
            var attributeName = attribute.Name.LocalName;
            if (attributeNameToParse == attributeName)
            {
                state.AppendParts(
                    CrefToSymbolDisplayParts(attribute.Value, state.Position, state.SemanticModel, state.Format, kind).ToTaggedText());
            }
            else
            {
                var displayKind = attributeName == DocumentationCommentXmlNames.LangwordAttributeName
                    ? TextTags.Keyword
                    : TextTags.Text;
                state.AppendParts(SpecializedCollections.SingletonEnumerable(new TaggedText(displayKind, attribute.Value)));
            }
        }

        internal static IEnumerable<SymbolDisplayPart> CrefToSymbolDisplayParts(
            string crefValue, int position, SemanticModel semanticModel, SymbolDisplayFormat format = null, SymbolDisplayPartKind kind = SymbolDisplayPartKind.Text)
        {
            // first try to parse the symbol
            if (semanticModel != null)
            {
                var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefValue, semanticModel.Compilation);
                if (symbol != null)
                {
                    format ??= SymbolDisplayFormat.MinimallyQualifiedFormat;
                    if (symbol.IsConstructor())
                    {
                        format = format.WithMemberOptions(SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface);
                    }

                    return symbol.ToMinimalDisplayParts(semanticModel, position, format);
                }
            }

            // if any of that fails fall back to just displaying the raw text
            return SpecializedCollections.SingletonEnumerable(
                new SymbolDisplayPart(kind, symbol: null, text: TrimCrefPrefix(crefValue)));
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
            for (var i = 0; i < rawText.Length; i++)
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
