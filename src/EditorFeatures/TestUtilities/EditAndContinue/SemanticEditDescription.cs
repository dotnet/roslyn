// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public sealed class SemanticEditDescription
    {
        public readonly SemanticEditKind Kind;
        public readonly Func<Compilation, ISymbol> SymbolProvider;
        public readonly Func<Compilation, ITypeSymbol>? PartialType;
        public readonly Func<Compilation, ISymbol>? DeletedSymbolContainerProvider;

        /// <summary>
        /// If specified the node mappings will be validated against the actual syntax map function.
        /// </summary>
        public readonly IEnumerable<KeyValuePair<TextSpan, TextSpan>>? SyntaxMap;

        public readonly bool HasSyntaxMap;

        public SemanticEditDescription(
            SemanticEditKind kind,
            Func<Compilation, ISymbol> symbolProvider,
            Func<Compilation, ITypeSymbol>? partialType,
            IEnumerable<KeyValuePair<TextSpan, TextSpan>>? syntaxMap,
            bool hasSyntaxMap,
            Func<Compilation, ISymbol>? deletedSymbolContainerProvider)
        {
            Kind = kind;
            SymbolProvider = symbolProvider;
            SyntaxMap = syntaxMap;
            PartialType = partialType;
            HasSyntaxMap = hasSyntaxMap;
            DeletedSymbolContainerProvider = deletedSymbolContainerProvider;
        }
    }
}
