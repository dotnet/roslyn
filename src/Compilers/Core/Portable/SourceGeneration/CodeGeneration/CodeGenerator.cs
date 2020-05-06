// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static SymbolModifiers GetModifiers(this ISymbol symbol)
        {
            if (symbol is Symbol codeGenSymbol)
                return codeGenSymbol.Modifiers;

            var result = SymbolModifiers.None;

            if (symbol.IsStatic)
                result |= SymbolModifiers.Static;

            if (symbol.IsAbstract)
                result |= SymbolModifiers.Abstract;

            if (symbol.IsOverride)
                result |= SymbolModifiers.Override;

            if (symbol.IsSealed)
                result |= SymbolModifiers.Sealed;

            if (symbol.IsExtern)
                result |= SymbolModifiers.Extern;

            // Add support for these as necessary:

            // only specifiable directly, can't be inferred from symbol.
            // New = 1 << 2,
            // Unsafe = 1 << 3,
            // Partial = 1 << 10,

            if (symbol is IFieldSymbol field)
            {
                if (field.IsReadOnly)
                    result |= SymbolModifiers.ReadOnly;

                if (field.IsConst)
                    result |= SymbolModifiers.Const;

                if (field.IsVolatile)
                    result |= SymbolModifiers.Volatile;
            }

            // could be inferred from symbol
            // WriteOnly = 1 << 12,

            // could be inferred from iops:
            // Async = 1 << 11,

            // Not sure.
            // WithEvents = 1 << 9,
            // Ref = 1 << 13,

            return result;
        }
    }
}
