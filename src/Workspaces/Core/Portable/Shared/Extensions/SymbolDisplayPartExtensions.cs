// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SymbolDisplayPartExtensions
    {
        public static string GetFullText(this ImmutableArray<SymbolDisplayPart> parts)
        {
            // CONSIDER: this does the same thing as parts.ToDisplayString(), but more slowly.
            return parts.AsEnumerable().GetFullText();
        }

        public static string GetFullText(this IEnumerable<SymbolDisplayPart> parts)
        {
            return string.Join(string.Empty, parts.Select(p => p.ToString()));
        }

        public static void AddAliasName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.AliasName, null, text));
        }

        public static void AddAssemblyName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.AssemblyName, null, text));
        }

        public static void AddClassName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ClassName, null, text));
        }

        public static void AddDelegateName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.DelegateName, null, text));
        }

        public static void AddEnumName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.EnumName, null, text));
        }

        public static void AddErrorTypeName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ErrorTypeName, null, text));
        }

        public static void AddEventName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.EventName, null, text));
        }

        public static void AddFieldName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.FieldName, null, text));
        }

        public static void AddInterfaceName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.InterfaceName, null, text));
        }

        public static void AddKeyword(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, text));
        }

        public static void AddLabelName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LabelName, null, text));
        }

        public static void AddLineBreak(this IList<SymbolDisplayPart> parts, string text = "\r\n")
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, text));
        }

        public static void AddNumericLiteral(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.NumericLiteral, null, text));
        }

        public static void AddStringLiteral(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.StringLiteral, null, text));
        }

        public static void AddLocalName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LocalName, null, text));
        }

        public static void AddMethodName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, null, text));
        }

        public static void AddModuleName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ModuleName, null, text));
        }

        public static void AddNamespaceName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.NamespaceName, null, text));
        }

        public static void AddOperator(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Operator, null, text));
        }

        public static void AddParameterName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, text));
        }

        public static void AddPropertyName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, null, text));
        }

        public static void AddPunctuation(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, text));
        }

        public static void AddRangeVariableName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.RangeVariableName, null, text));
        }

        public static void AddStructName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.StructName, null, text));
        }

        public static void AddSpace(this IList<SymbolDisplayPart> parts, string text = " ")
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, text));
        }

        public static void AddText(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text));
        }

        public static void AddTypeParameterName(this IList<SymbolDisplayPart> parts, string text)
        {
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.TypeParameterName, null, text));
        }
    }
}
