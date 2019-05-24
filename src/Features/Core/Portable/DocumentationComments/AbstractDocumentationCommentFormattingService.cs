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
        private enum DocumentationCommentListType
        {
            None,
            Bullet,
            Number,
            Table,
        }

        private class FormatterState
        {
            private bool _anyNonWhitespaceSinceLastPara;
            private bool _pendingParagraphBreak;
            private bool _pendingLineBreak;
            private bool _pendingSingleSpace;

            private static TaggedText s_spacePart = new TaggedText(TextTags.Space, " ");
            private static TaggedText s_newlinePart = new TaggedText(TextTags.LineBreak, "\r\n");

            internal readonly List<TaggedText> Builder = new List<TaggedText>();
            private readonly List<(DocumentationCommentListType type, int index, bool renderedItem)> _listStack = new List<(DocumentationCommentListType type, int index, bool renderedItem)>();
            private readonly Stack<TaggedTextStyle> _styleStack = new Stack<TaggedTextStyle>();

            public FormatterState()
            {
                _styleStack.Push(TaggedTextStyle.None);
            }

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

            internal TaggedTextStyle Style => _styleStack.Peek();

            public void AppendSingleSpace()
            {
                _pendingSingleSpace = true;
            }

            public void AppendString(string s)
            {
                EmitPendingChars();

                Builder.Add(new TaggedText(TextTags.Text, s, Style));

                _anyNonWhitespaceSinceLastPara = true;
            }

            public void AppendParts(IEnumerable<TaggedText> parts)
            {
                EmitPendingChars();

                Builder.AddRange(parts);

                _anyNonWhitespaceSinceLastPara = true;
            }

            public void PushList(DocumentationCommentListType listType)
            {
                _listStack.Add((listType, 0, false));
                MarkBeginOrEndPara();
            }

            public void NextListItem()
            {
                if (_listStack.Count == 0)
                {
                    return;
                }

                var (type, index, renderedItem) = _listStack[_listStack.Count - 1];
                if (renderedItem)
                {
                    Builder.Add(new TaggedText(TextTags.ContainerEnd, string.Empty));
                }

                _listStack[_listStack.Count - 1] = (type, index + 1, false);
                MarkLineBreak();
            }

            public void PopList()
            {
                if (_listStack.Count == 0)
                {
                    return;
                }

                if (_listStack[_listStack.Count - 1].renderedItem)
                {
                    Builder.Add(new TaggedText(TextTags.ContainerEnd, string.Empty));
                }

                _listStack.RemoveAt(_listStack.Count - 1);
                MarkBeginOrEndPara();
            }

            public void PushStyle(TaggedTextStyle style)
            {
                _styleStack.Push(_styleStack.Peek() | style);
            }

            public void PopStyle()
            {
                _styleStack.Pop();
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

                for (var i = 0; i < _listStack.Count; i++)
                {
                    if (_listStack[i].renderedItem)
                    {
                        continue;
                    }

                    switch (_listStack[i].type)
                    {
                        case DocumentationCommentListType.Bullet:
                            Builder.Add(new TaggedText(TextTags.ContainerStart, "• "));
                            break;

                        case DocumentationCommentListType.Number:
                            Builder.Add(new TaggedText(TextTags.ContainerStart, $"{_listStack[i].index}. "));
                            break;

                        case DocumentationCommentListType.Table:
                        case DocumentationCommentListType.None:
                        default:
                            Builder.Add(new TaggedText(TextTags.ContainerStart, string.Empty));
                            break;
                    }

                    _listStack[i] = (_listStack[i].type, _listStack[i].index, renderedItem: true);
                }
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
            var needPopStyle = false;

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
            else if (name == DocumentationCommentXmlNames.CElementName
                || name == DocumentationCommentXmlNames.CodeElementName
                || name == "tt")
            {
                needPopStyle = true;
                state.PushStyle(TaggedTextStyle.Code);
            }
            else if (name == "em" || name == "i")
            {
                needPopStyle = true;
                state.PushStyle(TaggedTextStyle.Emphasis);
            }
            else if (name == "strong" || name == "b" || name == DocumentationCommentXmlNames.TermElementName)
            {
                needPopStyle = true;
                state.PushStyle(TaggedTextStyle.Strong);
            }
            else if (name == "u")
            {
                needPopStyle = true;
                state.PushStyle(TaggedTextStyle.Underline);
            }

            if (name == DocumentationCommentXmlNames.ListElementName)
            {
                var rawListType = element.Attribute(DocumentationCommentXmlNames.TypeAttributeName)?.Value;
                DocumentationCommentListType listType;
                switch (rawListType)
                {
                    case "table":
                        listType = DocumentationCommentListType.Table;
                        break;

                    case "number":
                        listType = DocumentationCommentListType.Number;
                        break;

                    case "bullet":
                        listType = DocumentationCommentListType.Bullet;
                        break;

                    default:
                        listType = DocumentationCommentListType.None;
                        break;
                }

                state.PushList(listType);
            }
            else if (name == DocumentationCommentXmlNames.ItemElementName)
            {
                state.NextListItem();
            }

            if (name == DocumentationCommentXmlNames.ParaElementName
                || name == DocumentationCommentXmlNames.CodeElementName)
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

            if (name == DocumentationCommentXmlNames.ParaElementName
                || name == DocumentationCommentXmlNames.CodeElementName)
            {
                state.MarkBeginOrEndPara();
            }

            if (name == DocumentationCommentXmlNames.ListElementName)
            {
                state.PopList();
            }

            if (needPopStyle)
            {
                state.PopStyle();
            }

            if (name == DocumentationCommentXmlNames.TermElementName)
            {
                state.AppendSingleSpace();
                state.AppendString("–");
            }
        }

        private static void AppendTextFromAttribute(FormatterState state, XElement element, XAttribute attribute, string attributeNameToParse, SymbolDisplayPartKind kind)
        {
            var attributeName = attribute.Name.LocalName;
            if (attributeNameToParse == attributeName)
            {
                state.AppendParts(
                    CrefToSymbolDisplayParts(attribute.Value, state.Position, state.SemanticModel, state.Format, kind).ToTaggedText(state.Style));
            }
            else
            {
                var displayKind = attributeName == DocumentationCommentXmlNames.LangwordAttributeName
                    ? TextTags.Keyword
                    : TextTags.Text;
                state.AppendParts(SpecializedCollections.SingletonEnumerable(new TaggedText(displayKind, attribute.Value, state.Style)));
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
                    format = format ?? SymbolDisplayFormat.MinimallyQualifiedFormat;
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
