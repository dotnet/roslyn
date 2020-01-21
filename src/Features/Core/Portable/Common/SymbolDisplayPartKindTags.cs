// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolDisplayPartKindTags
    {
        public static string GetTag(SymbolDisplayPartKind kind)
            => kind switch
            {
                SymbolDisplayPartKind.AliasName => TextTags.Alias,
                SymbolDisplayPartKind.AssemblyName => TextTags.Assembly,
                SymbolDisplayPartKind.ClassName => TextTags.Class,
                SymbolDisplayPartKind.DelegateName => TextTags.Delegate,
                SymbolDisplayPartKind.EnumName => TextTags.Enum,
                SymbolDisplayPartKind.ErrorTypeName => TextTags.ErrorType,
                SymbolDisplayPartKind.EventName => TextTags.Event,
                SymbolDisplayPartKind.FieldName => TextTags.Field,
                SymbolDisplayPartKind.InterfaceName => TextTags.Interface,
                SymbolDisplayPartKind.Keyword => TextTags.Keyword,
                SymbolDisplayPartKind.LabelName => TextTags.Label,
                SymbolDisplayPartKind.LineBreak => TextTags.LineBreak,
                SymbolDisplayPartKind.NumericLiteral => TextTags.NumericLiteral,
                SymbolDisplayPartKind.StringLiteral => TextTags.StringLiteral,
                SymbolDisplayPartKind.LocalName => TextTags.Local,
                SymbolDisplayPartKind.MethodName => TextTags.Method,
                SymbolDisplayPartKind.ModuleName => TextTags.Module,
                SymbolDisplayPartKind.NamespaceName => TextTags.Namespace,
                SymbolDisplayPartKind.Operator => TextTags.Operator,
                SymbolDisplayPartKind.ParameterName => TextTags.Parameter,
                SymbolDisplayPartKind.PropertyName => TextTags.Property,
                SymbolDisplayPartKind.Punctuation => TextTags.Punctuation,
                SymbolDisplayPartKind.Space => TextTags.Space,
                SymbolDisplayPartKind.StructName => TextTags.Struct,
                SymbolDisplayPartKind.AnonymousTypeIndicator => TextTags.AnonymousTypeIndicator,
                SymbolDisplayPartKind.Text => TextTags.Text,
                SymbolDisplayPartKind.TypeParameterName => TextTags.TypeParameter,
                SymbolDisplayPartKind.RangeVariableName => TextTags.RangeVariable,
                SymbolDisplayPartKind.EnumMemberName => TextTags.EnumMember,
                SymbolDisplayPartKind.ExtensionMethodName => TextTags.ExtensionMethod,
                SymbolDisplayPartKind.ConstantName => TextTags.Constant,
                _ => string.Empty,
            };
    }
}
