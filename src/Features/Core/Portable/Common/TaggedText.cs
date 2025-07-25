// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A piece of text with a descriptive tag.
/// </summary>
[DataContract]
public readonly record struct TaggedText
{
    /// <summary>
    /// A descriptive tag from <see cref="TextTags"/>.
    /// </summary>
    [DataMember(Order = 0)]
    public string Tag { get; }

    /// <summary>
    /// The actual text to be displayed.
    /// </summary>
    [DataMember(Order = 1)]
    public string Text { get; }

    /// <summary>
    /// Gets the style(s) to apply to the text.
    /// </summary>
    [DataMember(Order = 2)]
    internal TaggedTextStyle Style { get; }

    /// <summary>
    /// Gets the navigation target for the text, or <see langword="null"/> if the text does not have a navigation
    /// target.
    /// </summary>
    [DataMember(Order = 3)]
    internal string? NavigationTarget { get; }

    /// <summary>
    /// Gets the navigation hint for the text, or <see langword="null"/> if the text does not have a navigation
    /// hint.
    /// </summary>
    [DataMember(Order = 4)]
    internal string? NavigationHint { get; }

    /// <summary>
    /// Creates a new instance of <see cref="TaggedText"/>
    /// </summary>
    /// <param name="tag">A descriptive tag from <see cref="TextTags"/>.</param>
    /// <param name="text">The actual text to be displayed.</param>
    public TaggedText(string tag, string text)
        : this(tag, text, TaggedTextStyle.None, navigationTarget: null, navigationHint: null)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="TaggedText"/>
    /// </summary>
    /// <param name="tag">A descriptive tag from <see cref="TextTags"/>.</param>
    /// <param name="text">The actual text to be displayed.</param>
    /// <param name="style">The style(s) to apply to the text.</param>
    /// <param name="navigationTarget">The navigation target for the text, or <see langword="null"/> if the text does not have a navigation target.</param>
    /// <param name="navigationHint">The navigation hint for the text, or <see langword="null"/> if the text does not have a navigation hint.</param>
    internal TaggedText(string tag, string text, TaggedTextStyle style, string? navigationTarget, string? navigationHint)
    {
        Tag = tag ?? throw new ArgumentNullException(nameof(tag));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Style = style;
        NavigationTarget = navigationTarget;
        NavigationHint = navigationHint;
    }

    public override string ToString()
        => Text;
}

internal static class TaggedTextExtensions
{
    extension(IEnumerable<SymbolDisplayPart>? displayParts)
    {
        public ImmutableArray<TaggedText> ToTaggedText(
TaggedTextStyle style = TaggedTextStyle.None, Func<ISymbol?, string?>? getNavigationHint = null, bool includeNavigationHints = true)
        {
            if (displayParts == null)
                return [];

            // To support CodeGeneration symbols, which do not support ToDisplayString we need to be able to override it.
            getNavigationHint ??= static symbol => symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return displayParts.SelectAsArray(d =>
                new TaggedText(
                    GetTag(d),
                    d.ToString(),
                    style,
                    includeNavigationHints && d.Kind != SymbolDisplayPartKind.NamespaceName ? GetNavigationTarget(d.Symbol) : null,
                    includeNavigationHints && d.Kind != SymbolDisplayPartKind.NamespaceName ? getNavigationHint(d.Symbol) : null));
        }
    }

    extension(ImmutableArray<SymbolDisplayPart> displayParts)
    {
        public ImmutableArray<(string tag, string text)> ToTagsAndText()
        => displayParts.SelectAsArray(static d => (GetTag(d), d.ToString()));
    }

    private static string GetTag(SymbolDisplayPart part)
    {
        // We don't actually have any specific classifications for aliases.  So if the compiler passed us that kind,
        // attempt to map to the corresponding namespace/named-type kind that matches the underlying alias target.
        if (part is { Symbol: IAliasSymbol alias, Kind: SymbolDisplayPartKind.AliasName })
        {
            if (alias.Target is INamespaceSymbol)
                return SymbolDisplayPartKindTags.GetTag(SymbolDisplayPartKind.NamespaceName);
            else if (alias.Target is INamedTypeSymbol namedType)
                return SymbolDisplayPartKindTags.GetTag(namedType.GetSymbolDisplayPartKind());
        }

        return SymbolDisplayPartKindTags.GetTag(part.Kind);
    }

    private static string? GetNavigationTarget(ISymbol? symbol)
        => symbol is null ? null : SymbolKey.CreateString(symbol);

    extension(ImmutableArray<TaggedText> values)
    {
        public string JoinText()
        {
            if (values.IsDefaultOrEmpty)
            {
                return "";
            }

            if (values is [var value])
            {
                return value.Text;
            }

            using var _ = PooledStringBuilder.GetInstance(out var builder);
            builder.EnsureCapacity(values.Sum(static value => value.Text.Length));
            foreach (var val in values)
            {
                builder.Append(val.Text);
            }

            return builder.ToString();
        }
    }

    extension(string taggedTextTag)
    {
        public string ToClassificationTypeName()
        => taggedTextTag switch
        {
            TextTags.Keyword => ClassificationTypeNames.Keyword,
            TextTags.Class => ClassificationTypeNames.ClassName,
            TextTags.Delegate => ClassificationTypeNames.DelegateName,
            TextTags.Enum => ClassificationTypeNames.EnumName,
            TextTags.Interface => ClassificationTypeNames.InterfaceName,
            TextTags.Module => ClassificationTypeNames.ModuleName,
            TextTags.Struct => ClassificationTypeNames.StructName,
            TextTags.TypeParameter => ClassificationTypeNames.TypeParameterName,
            TextTags.Field => ClassificationTypeNames.FieldName,
            TextTags.Event => ClassificationTypeNames.EventName,
            TextTags.Label => ClassificationTypeNames.LabelName,
            TextTags.Local => ClassificationTypeNames.LocalName,
            TextTags.Method => ClassificationTypeNames.MethodName,
            TextTags.Namespace => ClassificationTypeNames.NamespaceName,
            TextTags.Parameter => ClassificationTypeNames.ParameterName,
            TextTags.Property => ClassificationTypeNames.PropertyName,
            TextTags.ExtensionMethod => ClassificationTypeNames.ExtensionMethodName,
            TextTags.EnumMember => ClassificationTypeNames.EnumMemberName,
            TextTags.Constant => ClassificationTypeNames.ConstantName,
            TextTags.Alias or TextTags.Assembly or TextTags.ErrorType or TextTags.RangeVariable => ClassificationTypeNames.Identifier,
            TextTags.NumericLiteral => ClassificationTypeNames.NumericLiteral,
            TextTags.StringLiteral => ClassificationTypeNames.StringLiteral,
            TextTags.Space or TextTags.LineBreak => ClassificationTypeNames.WhiteSpace,
            TextTags.Operator => ClassificationTypeNames.Operator,
            TextTags.Punctuation => ClassificationTypeNames.Punctuation,
            TextTags.AnonymousTypeIndicator or TextTags.Text => ClassificationTypeNames.Text,
            TextTags.Record => ClassificationTypeNames.RecordClassName,
            TextTags.RecordStruct => ClassificationTypeNames.RecordStructName,
            // These tags are not visible so classify them as whitespace
            TextTags.ContainerStart or TextTags.ContainerEnd or TextTags.CodeBlockStart or TextTags.CodeBlockEnd => ClassificationTypeNames.WhiteSpace,
            _ => throw ExceptionUtilities.UnexpectedValue(taggedTextTag),
        };
    }

    extension(IEnumerable<TaggedText> parts)
    {
        public IEnumerable<ClassifiedSpan> ToClassifiedSpans(
)
        {
            var index = 0;
            foreach (var part in parts)
            {
                var text = part.ToString();
                var classificationTypeName = part.Tag.ToClassificationTypeName();

                yield return new ClassifiedSpan(new TextSpan(index, text.Length), classificationTypeName);
                index += text.Length;
            }
        }

        public string ToVisibleDisplayString(bool includeLeftToRightMarker)
        {
            return string.Join(string.Empty, parts.Select(
                p => p.ToVisibleDisplayString(includeLeftToRightMarker)));
        }

        public string GetFullText()
            => string.Join(string.Empty, parts.Select(p => p.ToString()));
    }

    private const string LeftToRightMarkerPrefix = "\u200e";

    extension(TaggedText part)
    {
        public string ToVisibleDisplayString(bool includeLeftToRightMarker)
        {
            var text = part.ToString();

            if (includeLeftToRightMarker)
            {
                var classificationTypeName = part.Tag.ToClassificationTypeName();
                if (classificationTypeName is ClassificationTypeNames.Punctuation or
                    ClassificationTypeNames.WhiteSpace)
                {
                    text = LeftToRightMarkerPrefix + text;
                }
            }

            return text;
        }
    }

    extension(IList<TaggedText> parts)
    {
        public void AddAliasName(string text)
        => parts.Add(new TaggedText(TextTags.Alias, text));

        public void AddAssemblyName(string text)
            => parts.Add(new TaggedText(TextTags.Assembly, text));

        public void AddClassName(string text)
            => parts.Add(new TaggedText(TextTags.Class, text));

        public void AddDelegateName(string text)
            => parts.Add(new TaggedText(TextTags.Delegate, text));

        public void AddEnumName(string text)
            => parts.Add(new TaggedText(TextTags.Enum, text));

        public void AddErrorTypeName(string text)
            => parts.Add(new TaggedText(TextTags.ErrorType, text));

        public void AddEventName(string text)
            => parts.Add(new TaggedText(TextTags.Event, text));

        public void AddFieldName(string text)
            => parts.Add(new TaggedText(TextTags.Field, text));

        public void AddInterfaceName(string text)
            => parts.Add(new TaggedText(TextTags.Interface, text));

        public void AddKeyword(string text)
            => parts.Add(new TaggedText(TextTags.Keyword, text));

        public void AddLabelName(string text)
            => parts.Add(new TaggedText(TextTags.Label, text));

        public void AddLineBreak(string text = "\r\n")
            => parts.Add(new TaggedText(TextTags.LineBreak, text));

        public void AddNumericLiteral(string text)
            => parts.Add(new TaggedText(TextTags.NumericLiteral, text));

        public void AddStringLiteral(string text)
            => parts.Add(new TaggedText(TextTags.StringLiteral, text));

        public void AddLocalName(string text)
            => parts.Add(new TaggedText(TextTags.Local, text));

        public void AddMethodName(string text)
            => parts.Add(new TaggedText(TextTags.Method, text));

        public void AddModuleName(string text)
            => parts.Add(new TaggedText(TextTags.Module, text));

        public void AddNamespaceName(string text)
            => parts.Add(new TaggedText(TextTags.Namespace, text));

        public void AddOperator(string text)
            => parts.Add(new TaggedText(TextTags.Operator, text));

        public void AddParameterName(string text)
            => parts.Add(new TaggedText(TextTags.Parameter, text));

        public void AddPropertyName(string text)
            => parts.Add(new TaggedText(TextTags.Property, text));

        public void AddPunctuation(string text)
            => parts.Add(new TaggedText(TextTags.Punctuation, text));

        public void AddRangeVariableName(string text)
            => parts.Add(new TaggedText(TextTags.RangeVariable, text));

        public void AddStructName(string text)
            => parts.Add(new TaggedText(TextTags.Struct, text));

        public void AddSpace(string text = " ")
            => parts.Add(new TaggedText(TextTags.Space, text));

        public void AddText(string text)
            => parts.Add(new TaggedText(TextTags.Text, text));

        public void AddTypeParameterName(string text)
            => parts.Add(new TaggedText(TextTags.TypeParameter, text));
    }
}
