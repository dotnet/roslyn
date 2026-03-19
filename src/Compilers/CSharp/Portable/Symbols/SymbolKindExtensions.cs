// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SymbolKindExtensions
    {
        public static LocalizableErrorArgument Localize(this SymbolKind kind)
        {
            switch (kind)
            {
                case SymbolKind.Namespace:
                    return MessageID.IDS_SK_NAMESPACE.Localize();
                case SymbolKind.NamedType:
                    return MessageID.IDS_SK_TYPE.Localize();
                case SymbolKind.TypeParameter:
                    return MessageID.IDS_SK_TYVAR.Localize();
                case SymbolKind.ArrayType:
                    return MessageID.IDS_SK_ARRAY.Localize();
                case SymbolKind.PointerType:
                    return MessageID.IDS_SK_POINTER.Localize();
                case SymbolKind.FunctionPointerType:
                    return MessageID.IDS_SK_FUNCTION_POINTER.Localize();
                case SymbolKind.DynamicType:
                    return MessageID.IDS_SK_DYNAMIC.Localize();
                case SymbolKind.Method:
                    return MessageID.IDS_SK_METHOD.Localize();
                case SymbolKind.Property:
                    return MessageID.IDS_SK_PROPERTY.Localize();
                case SymbolKind.Event:
                    return MessageID.IDS_SK_EVENT.Localize();
                case SymbolKind.Field:
                    return MessageID.IDS_SK_FIELD.Localize();
                case SymbolKind.Local:
                case SymbolKind.Parameter:
                case SymbolKind.RangeVariable:
                    return MessageID.IDS_SK_VARIABLE.Localize();
                case SymbolKind.Alias:
                    return MessageID.IDS_SK_ALIAS.Localize();
                case SymbolKind.Label:
                    return MessageID.IDS_SK_LABEL.Localize();
                case SymbolKind.Preprocessing:
                    throw ExceptionUtilities.UnexpectedValue(kind);
                default:
                    return MessageID.IDS_SK_UNKNOWN.Localize();
            }
        }
    }
}
