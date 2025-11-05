// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal abstract class AbstractDocumentationCommentFormattingService : IDocumentationCommentFormattingService
{
    private enum DocumentationCommentListType
    {
        None,
        Bullet,
        Number,
        Table,
    }

    private sealed class FormatterState
    {
        private bool _anyNonWhitespaceSinceLastPara;
        private bool _pendingParagraphBreak;
        private bool _pendingLineBreak;
        private bool _pendingSingleSpace;

        private static readonly TaggedText s_spacePart = new(TextTags.Space, " ");
        private static readonly TaggedText s_newlinePart = new(TextTags.LineBreak, "\r\n");

        internal readonly ImmutableArray<TaggedText>.Builder Builder = ImmutableArray.CreateBuilder<TaggedText>();

        /// <summary>
        /// Defines the containing lists for the current formatting state. The last item in the list is the
        /// innermost list.
        ///
        /// <list type="bullet">
        /// <item>
        /// <term><c>type</c></term>
        /// <description>The type of list.</description>
        /// </item>
        /// <item>
        /// <term><c>index</c></term>
        /// <description>The index of the current item in the list.</description>
        /// </item>
        /// <item>
        /// <term><c>renderedItem</c></term>
        /// <description><see langword="true"/> if the label (a bullet or number) for the current list item has already been rendered; otherwise <see langword="false"/>.</description>
        /// </item>
        /// </list>
        /// </summary>
        private readonly List<(DocumentationCommentListType type, int index, bool renderedItem)> _listStack = [];

        /// <summary>
        /// The top item of the stack indicates the hyperlink to apply to text rendered at the current location. It
        /// consists of a navigation <c>target</c> (the destination to navigate to when clicked) and a <c>hint</c>
        /// (typically shown as a tooltip for the link). This stack is never empty; when no hyperlink applies to the
        /// current scope, the top item of the stack will be a default tuple instance.
        /// </summary>
        private readonly Stack<(string target, string hint)> _navigationTargetStack = new();

        /// <summary>
        /// Tracks the style for text. The top item of the stack is the current style to apply (the merged result of
        /// all containing styles). This stack is never empty; when no style applies to the current scope, the top
        /// item of the stack will be <see cref="TaggedTextStyle.None"/>.
        /// </summary>
        private readonly Stack<TaggedTextStyle> _styleStack = new();

        public FormatterState()
        {
            _navigationTargetStack.Push(default);
            _styleStack.Push(TaggedTextStyle.None);
        }

        internal SemanticModel SemanticModel { get; set; }
        internal ISymbol TypeResolutionSymbol { get; set; }
        internal int Position { get; set; }
        internal StructuralTypeDisplayInfo TypeDisplayInfo { get; set; }

        public bool AtBeginning => Builder.Count == 0;

        public SymbolDisplayFormat Format { get; internal set; }

        internal (string target, string hint) NavigationTarget => _navigationTargetStack.Peek();
        internal TaggedTextStyle Style => _styleStack.Peek();

        public void AppendSingleSpace()
            => _pendingSingleSpace = true;

        public void AppendString(string s)
        {
            EmitPendingChars();
            Builder.Add(new TaggedText(TextTags.Text, NormalizeLineEndings(s), Style, NavigationTarget.target, NavigationTarget.hint));

            _anyNonWhitespaceSinceLastPara = true;

            // XText.Value returns a string with `\n` as the line endings, causing
            // the end result to have mixed line-endings. So normalize everything to `\r\n`.
            // https://www.w3.org/TR/xml/#sec-line-ends
            static string NormalizeLineEndings(string input) => input.Replace("\n", "\r\n");
        }

        public void AppendParts(IEnumerable<TaggedText> parts)
        {
            EmitPendingChars();

            Builder.AddRange(parts);

            _anyNonWhitespaceSinceLastPara = true;
        }

        public void PushList(DocumentationCommentListType listType)
        {
            _listStack.Add((listType, index: 0, renderedItem: false));
            MarkBeginOrEndPara();
        }

        /// <summary>
        /// Marks the start of an item in a list; called before each item.
        /// </summary>
        public void NextListItem()
        {
            if (_listStack.Count == 0)
            {
                return;
            }

            var (type, index, renderedItem) = _listStack[^1];
            if (renderedItem)
            {
                // Mark the end of the previous list item
                Builder.Add(new TaggedText(TextTags.ContainerEnd, string.Empty));
            }

            // The next list item has an incremented index, and has not yet been rendered to Builder.
            _listStack[^1] = (type, index + 1, renderedItem: false);
            MarkLineBreak();
        }

        public void PopList()
        {
            if (_listStack.Count == 0)
            {
                return;
            }

            if (_listStack[^1].renderedItem)
            {
                Builder.Add(new TaggedText(TextTags.ContainerEnd, string.Empty));
            }

            _listStack.RemoveAt(_listStack.Count - 1);
            MarkBeginOrEndPara();
        }

        public void PushNavigationTarget(string target, string hint)
            => _navigationTargetStack.Push((target, hint));

        public void PopNavigationTarget()
            => _navigationTargetStack.Pop();

        public void PushStyle(TaggedTextStyle style)
            => _styleStack.Push(_styleStack.Peek() | style);

        public void PopStyle()
            => _styleStack.Pop();

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
            => Builder.GetFullText();

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

    public ImmutableArray<TaggedText> Format(
        string rawXmlText,
        ISymbol symbol,
        SemanticModel semanticModel,
        int position,
        SymbolDisplayFormat format,
        StructuralTypeDisplayInfo typeDisplayInfo,
        CancellationToken cancellationToken)
    {
        if (rawXmlText is null)
            return [];

        var state = new FormatterState() { SemanticModel = semanticModel, Position = position, Format = format, TypeResolutionSymbol = symbol };

        // In case the XML is a fragment (that is, a series of elements without a parent)
        // wrap it up in a single tag. This makes parsing it much, much easier.
        var inputString = "<tag>" + rawXmlText + "</tag>";

        var summaryElement = XElement.Parse(inputString, LoadOptions.PreserveWhitespace);

        AppendTextFromNode(state, summaryElement, state.SemanticModel.Compilation);

        return state.Builder.ToImmutable();
    }

    private static void AppendTextFromNode(FormatterState state, XNode node, Compilation compilation)
    {
        if (node is XText textNode)
        {
            if (textNode.NodeType == XmlNodeType.Text)
            {
                AppendTextFromTextNode(state, textNode, replaceNewLineWithPara: false);
            }
            else if (textNode.NodeType == XmlNodeType.CDATA)
            {
                state.PushStyle(TaggedTextStyle.Code | TaggedTextStyle.PreserveWhitespace);
                AppendTextFromTextNode(state, textNode, replaceNewLineWithPara: true);
                state.PopStyle();
            }

            return;
        }

        if (node.NodeType != XmlNodeType.Element)
        {
            return;
        }

        var element = (XElement)node;

        var name = element.Name.LocalName;
        var needPopStyle = false;
        (string target, string hint)? navigationTarget = null;

        if (name is DocumentationCommentXmlNames.SeeElementName or
                    DocumentationCommentXmlNames.SeeAlsoElementName or
                    "a")
        {
            if (element.IsEmpty || element.FirstNode == null)
            {
                foreach (var attribute in element.Attributes())
                {
                    AppendTextFromAttribute(state, attribute, attributeNameToParse: DocumentationCommentXmlNames.CrefAttributeName, SymbolDisplayPartKind.Text);
                }

                return;
            }
            else
            {
                navigationTarget = GetNavigationTarget(element, state.SemanticModel, state.Position, state.Format);
                if (navigationTarget is object)
                {
                    state.PushNavigationTarget(navigationTarget.Value.target, navigationTarget.Value.hint);
                }
            }
        }
        else if (name is DocumentationCommentXmlNames.ParameterReferenceElementName or
                         DocumentationCommentXmlNames.TypeParameterReferenceElementName)
        {
            var kind = name == DocumentationCommentXmlNames.ParameterReferenceElementName ? SymbolDisplayPartKind.ParameterName : SymbolDisplayPartKind.TypeParameterName;
            foreach (var attribute in element.Attributes())
            {
                AppendTextFromAttribute(state, attribute, attributeNameToParse: DocumentationCommentXmlNames.NameAttributeName, kind);
            }

            return;
        }
        else if (name is DocumentationCommentXmlNames.CElementName or DocumentationCommentXmlNames.TtElementName)
        {
            needPopStyle = true;
            state.PushStyle(TaggedTextStyle.Code);
        }
        else if (name is DocumentationCommentXmlNames.CodeElementName)
        {
            needPopStyle = true;
            state.PushStyle(TaggedTextStyle.Code | TaggedTextStyle.PreserveWhitespace);
        }
        else if (name is DocumentationCommentXmlNames.EmElementName or DocumentationCommentXmlNames.IElementName)
        {
            needPopStyle = true;
            state.PushStyle(TaggedTextStyle.Emphasis);
        }
        else if (name is DocumentationCommentXmlNames.StrongElementName or DocumentationCommentXmlNames.BElementName or DocumentationCommentXmlNames.TermElementName)
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
            var listType = rawListType switch
            {
                "table" => DocumentationCommentListType.Table,
                "number" => DocumentationCommentListType.Number,
                "bullet" => DocumentationCommentListType.Bullet,
                _ => DocumentationCommentListType.None,
            };
            state.PushList(listType);
        }
        else if (name == DocumentationCommentXmlNames.ItemElementName)
        {
            state.NextListItem();
        }

        if (name is DocumentationCommentXmlNames.ParaElementName or
                    DocumentationCommentXmlNames.CodeElementName)
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

        if (name is DocumentationCommentXmlNames.ParaElementName or
                    DocumentationCommentXmlNames.CodeElementName)
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

        if (navigationTarget is object)
        {
            state.PopNavigationTarget();
        }

        if (name == DocumentationCommentXmlNames.TermElementName)
        {
            state.AppendSingleSpace();
            state.AppendString("–");
        }
    }

    private static (string target, string hint)? GetNavigationTarget(XElement element, SemanticModel semanticModel, int position, SymbolDisplayFormat format)
    {
        var crefAttribute = element.Attribute(DocumentationCommentXmlNames.CrefAttributeName);
        if (crefAttribute is not null && semanticModel is not null)
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefAttribute.Value, semanticModel.Compilation);
            if (symbol is not null)
                return (target: SymbolKey.CreateString(symbol), hint: symbol.ToMinimalDisplayString(semanticModel, position, format ?? SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        var hrefAttribute = element.Attribute(DocumentationCommentXmlNames.HrefAttributeName);
        if (hrefAttribute is not null)
            return (target: hrefAttribute.Value, hint: hrefAttribute.Value);

        return null;
    }

    private static void AppendTextFromAttribute(FormatterState state, XAttribute attribute, string attributeNameToParse, SymbolDisplayPartKind kind)
    {
        var attributeName = attribute.Name.LocalName;
        if (attributeNameToParse == attributeName)
        {
            if (kind == SymbolDisplayPartKind.TypeParameterName)
            {
                state.AppendParts(
                    TypeParameterRefToSymbolDisplayParts(attribute.Value, state).ToTaggedText(state.Style));
            }
            else
            {
                state.AppendParts(
                    CrefToSymbolDisplayParts(attribute.Value, state.Position, state.SemanticModel, state.TypeDisplayInfo, state.Format, kind).ToTaggedText(state.Style));
            }
        }
        else
        {
            var displayKind = attributeName == DocumentationCommentXmlNames.LangwordAttributeName
                ? TextTags.Keyword
                : TextTags.Text;
            var text = attribute.Value;
            var style = state.Style;
            var navigationTarget = attributeName == DocumentationCommentXmlNames.HrefAttributeName
                ? attribute.Value
                : null;
            var navigationHint = navigationTarget;
            state.AppendParts([new TaggedText(displayKind, text, style, navigationTarget, navigationHint)]);
        }
    }

    internal static IEnumerable<SymbolDisplayPart> CrefToSymbolDisplayParts(
        string crefValue, int position, SemanticModel semanticModel, StructuralTypeDisplayInfo typeDisplayInfo, SymbolDisplayFormat format = null, SymbolDisplayPartKind kind = SymbolDisplayPartKind.Text)
    {
        // first try to parse the symbol
        if (crefValue != null && semanticModel != null)
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefValue, semanticModel.Compilation);
            if (symbol != null)
            {
                format ??= SymbolDisplayFormat.MinimallyQualifiedFormat;
                if (symbol.IsConstructor())
                {
                    format = format.WithMemberOptions(SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface);
                }

                var parts = symbol.ToMinimalDisplayParts(semanticModel, position, format);
                return typeDisplayInfo.ReplaceStructuralTypes(parts, semanticModel, position);
            }
        }

        // if any of that fails fall back to just displaying the raw text
        return [new SymbolDisplayPart(kind, symbol: null, text: TrimCrefPrefix(crefValue))];
    }

    private static IEnumerable<SymbolDisplayPart> TypeParameterRefToSymbolDisplayParts(
        string crefValue, FormatterState state)
    {
        var typeResolutionSymbol = state.TypeResolutionSymbol;
        var semanticModel = state.SemanticModel;
        var position = state.Position;
        var format = state.Format;

        if (semanticModel != null)
        {
            var typeParameterIndex = typeResolutionSymbol.OriginalDefinition.GetAllTypeParameters().IndexOf(tp => tp.Name == crefValue);
            if (typeParameterIndex >= 0)
            {
                var typeArgs = typeResolutionSymbol.GetAllTypeArguments();
                if (typeArgs.Length > typeParameterIndex)
                {
                    var parts = typeArgs[typeParameterIndex].ToMinimalDisplayParts(semanticModel, position, format);
                    return state.TypeDisplayInfo.ReplaceStructuralTypes(parts, semanticModel, position);
                }
            }
        }

        // if any of that fails fall back to just displaying the raw text
        return [new SymbolDisplayPart(SymbolDisplayPartKind.TypeParameterName, symbol: null, text: TrimCrefPrefix(crefValue))];
    }

    private static string TrimCrefPrefix(string value)
    {
        if (value is [_, ':', ..])
            value = value[2..];

        return value;
    }

    private static void AppendTextFromTextNode(FormatterState state, XText element, bool replaceNewLineWithPara)
    {
        var rawText = element.Value;
        if ((state.Style & TaggedTextStyle.PreserveWhitespace) == TaggedTextStyle.PreserveWhitespace)
        {
            if (replaceNewLineWithPara && rawText is ['\n', ..])
                state.MarkBeginOrEndPara();

            state.AppendString(rawText.Trim('\n'));

            if (replaceNewLineWithPara && rawText is [.., '\n'])
                state.MarkBeginOrEndPara();

            return;
        }

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
