// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    [Flags]
    internal enum SymbolModifiers
    {
#pragma warning disable format
        None        = 0,
        Static      = 1 << 0,
        Abstract    = 1 << 1,
        New         = 1 << 2,
        Unsafe      = 1 << 3,
        ReadOnly    = 1 << 4,
        Virtual     = 1 << 5,
        Override    = 1 << 6,
        Sealed      = 1 << 7,
        Const       = 1 << 8,
        WithEvents  = 1 << 9,
        Partial     = 1 << 10,
        Async       = 1 << 11,
        WriteOnly   = 1 << 12,
        Ref         = 1 << 13,
        Volatile    = 1 << 14,
        Extern      = 1 << 15,
#pragma warning restore format
    }

    internal static class SymbolModifiersExtensions
    {
        public static bool HasFlag(this SymbolModifiers modifiers, SymbolModifiers other)
            => other != SymbolModifiers.None && (modifiers & other) == other;
    }
}
