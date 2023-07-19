// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class CSharpSymbolChanges : SymbolChanges
    {
        public CSharpSymbolChanges(DefinitionMap definitionMap, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol)
            : base(definitionMap, edits, isAddedSymbol)
        { }

        protected override ISymbolInternal? GetISymbolInternalOrNull(ISymbol symbol)
        {
            return (symbol as Symbols.PublicModel.Symbol)?.UnderlyingSymbol;
        }
    }
}
