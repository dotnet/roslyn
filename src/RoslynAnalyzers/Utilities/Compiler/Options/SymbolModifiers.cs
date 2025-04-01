// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Describes a group of modifiers for symbol declaration.
    /// </summary>
    [Flags]
    public enum SymbolModifiers
    {
        // NOTE: Below fields names are used in the .editorconfig specification
        //       for symbol modifiers analyzer option. Hence the names should *not* be modified,
        //       as that would be a breaking change for .editorconfig specification.
        None = 0x0000,
        Static = 0x0001,
        Shared = Static,
        Const = 0x0002,
        ReadOnly = 0x0004,
        Abstract = 0x0008,
        Virtual = 0x0010,
        Override = 0x0020,
        Sealed = 0x0040,
        Extern = 0x0080,
        Async = 0x0100,
    }

    internal static class SymbolModifiersExtensions
    {
        public static bool Contains(this SymbolModifiers modifiers, SymbolModifiers modifiersToCheck)
            => (modifiers & modifiersToCheck) == modifiersToCheck;

        public static SymbolModifiers GetSymbolModifiers(this ISymbol symbol)
        {
            var modifiers = SymbolModifiers.None;
            if (symbol.IsStatic)
            {
                modifiers |= SymbolModifiers.Static;
            }

            if (symbol.IsConst())
            {
                modifiers |= SymbolModifiers.Const;
            }

            if (symbol.IsReadOnly())
            {
                modifiers |= SymbolModifiers.ReadOnly;
            }

            if (symbol.IsAbstract)
            {
                modifiers |= SymbolModifiers.Abstract;
            }

            if (symbol.IsVirtual)
            {
                modifiers |= SymbolModifiers.Virtual;
            }

            if (symbol.IsOverride)
            {
                modifiers |= SymbolModifiers.Override;
            }

            if (symbol.IsSealed)
            {
                modifiers |= SymbolModifiers.Sealed;
            }

            if (symbol.IsExtern)
            {
                modifiers |= SymbolModifiers.Extern;
            }

            if (symbol is IMethodSymbol method &&
                method.IsAsync)
            {
                modifiers |= SymbolModifiers.Async;
            }

            return modifiers;
        }
    }
}
