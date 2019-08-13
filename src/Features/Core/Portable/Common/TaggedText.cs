// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A piece of text with a descriptive tag.
    /// </summary>
    public readonly struct TaggedText
    {
        /// <summary>
        /// A descriptive tag from <see cref="TextTags"/>.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// The actual text to be displayed.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates a new instance of <see cref="TaggedText"/>
        /// </summary>
        /// <param name="tag">A descriptive tag from <see cref="TextTags"/>.</param>
        /// <param name="text">The actual text to be displayed.</param>
        public TaggedText(string tag, string text)
        {
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public override string ToString()
        {
            return Text;
        }
    }

    internal static class TaggedTextExtensions
    {
        public static ImmutableArray<TaggedText> ToTaggedText(this IEnumerable<SymbolDisplayPart> displayParts)
        {
            if (displayParts == null)
            {
                return ImmutableArray<TaggedText>.Empty;
            }

            return displayParts.Select(d =>
                new TaggedText(SymbolDisplayPartKindTags.GetTag(d.Kind), d.ToString())).ToImmutableArray();
        }

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
                if (classificationTypeName == ClassificationTypeNames.Punctuation ||
                    classificationTypeName == ClassificationTypeNames.WhiteSpace)
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
        {
            return string.Join(string.Empty, parts.Select(p => p.ToString()));
        }

        public static void AddAliasName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Alias, text));
        }

        public static void AddAssemblyName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Assembly, text));
        }

        public static void AddClassName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Class, text));
        }

        public static void AddDelegateName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Delegate, text));
        }

        public static void AddEnumName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Enum, text));
        }

        public static void AddErrorTypeName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.ErrorType, text));
        }

        public static void AddEventName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Event, text));
        }

        public static void AddFieldName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Field, text));
        }

        public static void AddInterfaceName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Interface, text));
        }

        public static void AddKeyword(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Keyword, text));
        }

        public static void AddLabelName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Label, text));
        }

        public static void AddLineBreak(this IList<TaggedText> parts, string text = "\r\n")
        {
            parts.Add(new TaggedText(TextTags.LineBreak, text));
        }

        public static void AddNumericLiteral(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.NumericLiteral, text));
        }

        public static void AddStringLiteral(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.StringLiteral, text));
        }

        public static void AddLocalName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Local, text));
        }

        public static void AddMethodName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Method, text));
        }

        public static void AddModuleName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Module, text));
        }

        public static void AddNamespaceName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Namespace, text));
        }

        public static void AddOperator(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Operator, text));
        }

        public static void AddParameterName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Parameter, text));
        }

        public static void AddPropertyName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Property, text));
        }

        public static void AddPunctuation(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Punctuation, text));
        }

        public static void AddRangeVariableName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.RangeVariable, text));
        }

        public static void AddStructName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Struct, text));
        }

        public static void AddSpace(this IList<TaggedText> parts, string text = " ")
        {
            parts.Add(new TaggedText(TextTags.Space, text));
        }

        public static void AddText(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.Text, text));
        }

        public static void AddTypeParameterName(this IList<TaggedText> parts, string text)
        {
            parts.Add(new TaggedText(TextTags.TypeParameter, text));
        }
    }
}
