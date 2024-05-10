// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis;

internal static class SymbolDisplayPartKindTags
{
    public static SymbolDisplayPartKind GetSymbolDisplayPartKind(this INamedTypeSymbol namedType)
    {
        if (namedType.IsEnumType())
            return SymbolDisplayPartKind.EnumName;

        if (namedType.IsDelegateType())
            return SymbolDisplayPartKind.DelegateName;

        if (namedType.IsInterfaceType())
            return SymbolDisplayPartKind.InterfaceName;

        if (namedType.IsRecord)
            return namedType.IsValueType ? SymbolDisplayPartKind.RecordStructName : SymbolDisplayPartKind.RecordClassName;

        if (namedType.IsStructType())
            return SymbolDisplayPartKind.StructName;

        if (namedType.IsModuleType())
            return SymbolDisplayPartKind.ModuleName;

        if (namedType.IsErrorType())
            return SymbolDisplayPartKind.ErrorTypeName;

        return SymbolDisplayPartKind.ClassName;
    }

    public static string GetTag(SymbolDisplayPartKind kind)
        => kind switch
        {
            SymbolDisplayPartKind.AliasName => TextTags.Alias,
            SymbolDisplayPartKind.AnonymousTypeIndicator => TextTags.AnonymousTypeIndicator,
            SymbolDisplayPartKind.AssemblyName => TextTags.Assembly,
            SymbolDisplayPartKind.ClassName => TextTags.Class,
            SymbolDisplayPartKind.ConstantName => TextTags.Constant,
            SymbolDisplayPartKind.DelegateName => TextTags.Delegate,
            SymbolDisplayPartKind.EnumMemberName => TextTags.EnumMember,
            SymbolDisplayPartKind.EnumName => TextTags.Enum,
            SymbolDisplayPartKind.ErrorTypeName => TextTags.ErrorType,
            SymbolDisplayPartKind.EventName => TextTags.Event,
            SymbolDisplayPartKind.ExtensionMethodName => TextTags.ExtensionMethod,
            SymbolDisplayPartKind.ExtensionName => TextTags.Extension,
            SymbolDisplayPartKind.FieldName => TextTags.Field,
            SymbolDisplayPartKind.InterfaceName => TextTags.Interface,
            SymbolDisplayPartKind.Keyword => TextTags.Keyword,
            SymbolDisplayPartKind.LabelName => TextTags.Label,
            SymbolDisplayPartKind.LineBreak => TextTags.LineBreak,
            SymbolDisplayPartKind.LocalName => TextTags.Local,
            SymbolDisplayPartKind.MethodName => TextTags.Method,
            SymbolDisplayPartKind.ModuleName => TextTags.Module,
            SymbolDisplayPartKind.NamespaceName => TextTags.Namespace,
            SymbolDisplayPartKind.NumericLiteral => TextTags.NumericLiteral,
            SymbolDisplayPartKind.Operator => TextTags.Operator,
            SymbolDisplayPartKind.ParameterName => TextTags.Parameter,
            SymbolDisplayPartKind.PropertyName => TextTags.Property,
            SymbolDisplayPartKind.Punctuation => TextTags.Punctuation,
            SymbolDisplayPartKind.RangeVariableName => TextTags.RangeVariable,
            SymbolDisplayPartKind.RecordClassName => TextTags.Record,
            SymbolDisplayPartKind.RecordStructName => TextTags.RecordStruct,
            SymbolDisplayPartKind.Space => TextTags.Space,
            SymbolDisplayPartKind.StringLiteral => TextTags.StringLiteral,
            SymbolDisplayPartKind.StructName => TextTags.Struct,
            SymbolDisplayPartKind.Text => TextTags.Text,
            SymbolDisplayPartKind.TypeParameterName => TextTags.TypeParameter,
            _ => string.Empty,
        };
}
