// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SymbolDisplayPartKindExtensions
    {
        public static string ToClassificationTypeName(
            this SymbolDisplayPartKind kind)
        {
            switch (kind)
            {
                case SymbolDisplayPartKind.Keyword:
                    return ClassificationTypeNames.Keyword;

                case SymbolDisplayPartKind.ClassName:
                    return ClassificationTypeNames.ClassName;

                case SymbolDisplayPartKind.DelegateName:
                    return ClassificationTypeNames.DelegateName;

                case SymbolDisplayPartKind.EnumName:
                    return ClassificationTypeNames.EnumName;

                case SymbolDisplayPartKind.InterfaceName:
                    return ClassificationTypeNames.InterfaceName;

                case SymbolDisplayPartKind.ModuleName:
                    return ClassificationTypeNames.ModuleName;

                case SymbolDisplayPartKind.StructName:
                    return ClassificationTypeNames.StructName;

                case SymbolDisplayPartKind.TypeParameterName:
                    return ClassificationTypeNames.TypeParameterName;

                case SymbolDisplayPartKind.AliasName:
                case SymbolDisplayPartKind.AssemblyName:
                case SymbolDisplayPartKind.FieldName:
                case SymbolDisplayPartKind.ErrorTypeName:
                case SymbolDisplayPartKind.EventName:
                case SymbolDisplayPartKind.LabelName:
                case SymbolDisplayPartKind.LocalName:
                case SymbolDisplayPartKind.MethodName:
                case SymbolDisplayPartKind.NamespaceName:
                case SymbolDisplayPartKind.ParameterName:
                case SymbolDisplayPartKind.PropertyName:
                case SymbolDisplayPartKind.RangeVariableName:
                    return ClassificationTypeNames.Identifier;

                case SymbolDisplayPartKind.NumericLiteral:
                    return ClassificationTypeNames.NumericLiteral;

                case SymbolDisplayPartKind.StringLiteral:
                    return ClassificationTypeNames.StringLiteral;

                case SymbolDisplayPartKind.Space:
                case SymbolDisplayPartKind.LineBreak:
                    return ClassificationTypeNames.WhiteSpace;

                case SymbolDisplayPartKind.Operator:
                    return ClassificationTypeNames.Operator;

                case SymbolDisplayPartKind.Punctuation:
                    return ClassificationTypeNames.Punctuation;

                case SymbolDisplayPartKind.AnonymousTypeIndicator:
                case SymbolDisplayPartKind.Text:
                    return ClassificationTypeNames.Text;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
