// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolDisplayPartKindTags
    {
        public static string GetTag(SymbolDisplayPartKind kind)
        {
            switch (kind)
            {
                case SymbolDisplayPartKind.AliasName:
                    return TextTags.Alias;
                case SymbolDisplayPartKind.AssemblyName:
                    return TextTags.Assembly;
                case SymbolDisplayPartKind.ClassName:
                    return TextTags.Class;
                case SymbolDisplayPartKind.DelegateName:
                    return TextTags.Delegate;
                case SymbolDisplayPartKind.EnumName:
                    return TextTags.Enum;
                case SymbolDisplayPartKind.ErrorTypeName:
                    return TextTags.ErrorType;
                case SymbolDisplayPartKind.EventName:
                    return TextTags.Event;
                case SymbolDisplayPartKind.FieldName:
                    return TextTags.Field;
                case SymbolDisplayPartKind.InterfaceName:
                    return TextTags.Interface;
                case SymbolDisplayPartKind.Keyword:
                    return TextTags.Keyword;
                case SymbolDisplayPartKind.LabelName:
                    return TextTags.Label;
                case SymbolDisplayPartKind.LineBreak:
                    return TextTags.LineBreak;
                case SymbolDisplayPartKind.NumericLiteral:
                    return TextTags.NumericLiteral;
                case SymbolDisplayPartKind.StringLiteral:
                    return TextTags.StringLiteral;
                case SymbolDisplayPartKind.LocalName:
                    return TextTags.Local;
                case SymbolDisplayPartKind.MethodName:
                    return TextTags.Method;
                case SymbolDisplayPartKind.ModuleName:
                    return TextTags.Module;
                case SymbolDisplayPartKind.NamespaceName:
                    return TextTags.Namespace;
                case SymbolDisplayPartKind.Operator:
                    return TextTags.Operator;
                case SymbolDisplayPartKind.ParameterName:
                    return TextTags.Parameter;
                case SymbolDisplayPartKind.PropertyName:
                    return TextTags.Property;
                case SymbolDisplayPartKind.Punctuation:
                    return TextTags.Punctuation;
                case SymbolDisplayPartKind.Space:
                    return TextTags.Space;
                case SymbolDisplayPartKind.StructName:
                    return TextTags.Struct;
                case SymbolDisplayPartKind.AnonymousTypeIndicator:
                    return TextTags.AnonymousTypeIndicator;
                case SymbolDisplayPartKind.Text:
                    return TextTags.Text;
                case SymbolDisplayPartKind.TypeParameterName:
                    return TextTags.TypeParameter;
                case SymbolDisplayPartKind.RangeVariableName:
                    return TextTags.RangeVariable;
                case SymbolDisplayPartKind.EnumMemberName:
                    return TextTags.EnumMember;
                case SymbolDisplayPartKind.ExtensionMethodName:
                    return TextTags.ExtensionMethod;
                case SymbolDisplayPartKind.ConstantName:
                    return TextTags.Constant;
                default:
                    return string.Empty;
            }
        }
    }
}
