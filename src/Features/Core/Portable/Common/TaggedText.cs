// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
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
        internal string NavigationTarget { get; }

        /// <summary>
        /// Gets the navigation hint for the text, or <see langword="null"/> if the text does not have a navigation
        /// hint.
        /// </summary>
        [DataMember(Order = 4)]
        internal string NavigationHint { get; }

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
        internal TaggedText(string tag, string text, TaggedTextStyle style, string navigationTarget, string navigationHint)
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
        public static ImmutableArray<TaggedText> ToTaggedText(this IEnumerable<SymbolDisplayPart> displayParts, Func<ISymbol, string> getNavigationHint = null, bool includeNavigationHints = true)
            => displayParts.ToTaggedText(TaggedTextStyle.None, getNavigationHint, includeNavigationHints);

        public static ImmutableArray<TaggedText> ToTaggedText(
            this IEnumerable<SymbolDisplayPart> displayParts, TaggedTextStyle style, Func<ISymbol, string> getNavigationHint = null, bool includeNavigationHints = true)
        {
            if (displayParts == null)
                return ImmutableArray<TaggedText>.Empty;

            getNavigationHint ??= static symbol => symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return displayParts.SelectAsArray(d =>
                new TaggedText(
                    GetTag(d),
                    d.ToString(),
                    style,
                    includeNavigationHints && d.Kind != SymbolDisplayPartKind.NamespaceName ? GetNavigationTarget(d.Symbol) : null,
                    includeNavigationHints && d.Kind != SymbolDisplayPartKind.NamespaceName ? getNavigationHint(d.Symbol) : null));
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

        private static string GetNavigationTarget(ISymbol symbol)
            => symbol is null ? null : SymbolKey.CreateString(symbol);

        public static string JoinText(this ImmutableArray<TaggedText> values)
        {

            return values.IsDefault
                ? null
                : Join(values);
        }

        private static string Join(ImmutableArray<TaggedText> values)
        {
            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;
            foreach (var val in values)
            {
                builder.Append(val.Text);
            }

            return pooled.ToStringAndFree();
        }

        public static string ToClassificationTypeName(this string taggedTextTag)
        {
            switch (taggedTextTag)
            {
                case TextTags.Keyword:
                    return ClassificationTypeNames.Keyword;

                case TextTags.Class:
                    return ClassificationTypeNames.ClassName;

                case TextTags.Delegate:
                    return ClassificationTypeNames.DelegateName;

                case TextTags.Enum:
                    return ClassificationTypeNames.EnumName;

                case TextTags.Interface:
                    return ClassificationTypeNames.InterfaceName;

                case TextTags.Module:
                    return ClassificationTypeNames.ModuleName;

                case TextTags.Struct:
                    return ClassificationTypeNames.StructName;

                case TextTags.TypeParameter:
                    return ClassificationTypeNames.TypeParameterName;

                case TextTags.Field:
                    return ClassificationTypeNames.FieldName;

                case TextTags.Event:
                    return ClassificationTypeNames.EventName;

                case TextTags.Label:
                    return ClassificationTypeNames.LabelName;

                case TextTags.Local:
                    return ClassificationTypeNames.LocalName;

                case TextTags.Method:
                    return ClassificationTypeNames.MethodName;

                case TextTags.Namespace:
                    return ClassificationTypeNames.NamespaceName;

                case TextTags.Parameter:
                    return ClassificationTypeNames.ParameterName;

                case TextTags.Property:
                    return ClassificationTypeNames.PropertyName;

                case TextTags.ExtensionMethod:
                    return ClassificationTypeNames.ExtensionMethodName;

                case TextTags.EnumMember:
                    return ClassificationTypeNames.EnumMemberName;

                case TextTags.Constant:
                    return ClassificationTypeNames.ConstantName;

                case TextTags.Alias:
                case TextTags.Assembly:
                case TextTags.ErrorType:
                case TextTags.RangeVariable:
                    return ClassificationTypeNames.Identifier;

                case TextTags.NumericLiteral:
                    return ClassificationTypeNames.NumericLiteral;

                case TextTags.StringLiteral:
                    return ClassificationTypeNames.StringLiteral;

                case TextTags.Space:
                case TextTags.LineBreak:
                    return ClassificationTypeNames.WhiteSpace;

                case TextTags.Operator:
                    return ClassificationTypeNames.Operator;

                case TextTags.Punctuation:
                    return ClassificationTypeNames.Punctuation;

                case TextTags.AnonymousTypeIndicator:
                case TextTags.Text:
                    return ClassificationTypeNames.Text;

                case TextTags.Record:
                    return ClassificationTypeNames.RecordClassName;

                case TextTags.RecordStruct:
                    return ClassificationTypeNames.RecordStructName;

                case TextTags.ContainerStart:
                case TextTags.ContainerEnd:
                case TextTags.CodeBlockStart:
                case TextTags.CodeBlockEnd:
                    // These tags are not visible so classify them as whitespace
                    return ClassificationTypeNames.WhiteSpace;

                default:
                    throw ExceptionUtilities.UnexpectedValue(taggedTextTag);
            }
        }

        public static IEnumerable<ClassifiedSpan> ToClassifiedSpans(
            this IEnumerable<TaggedText> parts)
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

        private const string LeftToRightMarkerPrefix = "\u200e";

        public static string ToVisibleDisplayString(this TaggedText part, bool includeLeftToRightMarker)
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

        public static string ToVisibleDisplayString(this IEnumerable<TaggedText> parts, bool includeLeftToRightMarker)
        {
            return string.Join(string.Empty, parts.Select(
                p => p.ToVisibleDisplayString(includeLeftToRightMarker)));
        }

        public static string GetFullText(this IEnumerable<TaggedText> parts)
            => string.Join(string.Empty, parts.Select(p => p.ToString()));

        public static void AddAliasName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Alias, text));

        public static void AddAssemblyName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Assembly, text));

        public static void AddClassName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Class, text));

        public static void AddDelegateName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Delegate, text));

        public static void AddEnumName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Enum, text));

        public static void AddErrorTypeName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.ErrorType, text));

        public static void AddEventName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Event, text));

        public static void AddFieldName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Field, text));

        public static void AddInterfaceName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Interface, text));

        public static void AddKeyword(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Keyword, text));

        public static void AddLabelName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Label, text));

        public static void AddLineBreak(this IList<TaggedText> parts, string text = "\r\n")
            => parts.Add(new TaggedText(TextTags.LineBreak, text));

        public static void AddNumericLiteral(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.NumericLiteral, text));

        public static void AddStringLiteral(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.StringLiteral, text));

        public static void AddLocalName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Local, text));

        public static void AddMethodName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Method, text));

        public static void AddModuleName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Module, text));

        public static void AddNamespaceName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Namespace, text));

        public static void AddOperator(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Operator, text));

        public static void AddParameterName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Parameter, text));

        public static void AddPropertyName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Property, text));

        public static void AddPunctuation(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Punctuation, text));

        public static void AddRangeVariableName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.RangeVariable, text));

        public static void AddStructName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Struct, text));

        public static void AddSpace(this IList<TaggedText> parts, string text = " ")
            => parts.Add(new TaggedText(TextTags.Space, text));

        public static void AddText(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.Text, text));

        public static void AddTypeParameterName(this IList<TaggedText> parts, string text)
            => parts.Add(new TaggedText(TextTags.TypeParameter, text));
    }
}
