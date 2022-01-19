// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal sealed class SemanticEditDescription
    {
        public readonly SemanticEditKind Kind;
        public readonly Func<Compilation, ISymbol> SymbolProvider;

        public SemanticEditDescription(
            SemanticEditKind kind,
            Func<Compilation, ISymbol> symbolProvider)
        {
            Kind = kind;
            SymbolProvider = symbolProvider;
        }
    }
}
