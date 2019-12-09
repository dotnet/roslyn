// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolKindExtensions
    {
        public static int ToSortOrder(this SymbolKind kind)
        {
            switch (kind)
            {
                case SymbolKind.Field:
                    return 0;
                case SymbolKind.Method:
                    return 1;
                case SymbolKind.Property:
                    return 2;
                case SymbolKind.Event:
                    return 3;
                case SymbolKind.NamedType:
                    return 4;
                case SymbolKind.Namespace:
                    return 5;
                case SymbolKind.Alias:
                    return 6;
                case SymbolKind.ArrayType:
                    return 7;
                case SymbolKind.Assembly:
                    return 8;
#if false
                case SymbolKind.ErrorType:
                    return 9;
#endif
                case SymbolKind.Label:
                    return 10;
                case SymbolKind.Local:
                    return 11;
                case SymbolKind.NetModule:
                    return 12;
                case SymbolKind.Parameter:
                    return 13;
                case SymbolKind.RangeVariable:
                    return 14;
                case SymbolKind.TypeParameter:
                    return 15;
                case SymbolKind.PointerType:
                    return 16;
                case SymbolKind.DynamicType:
                    return 17;
                case SymbolKind.Preprocessing:
                    return 18;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
