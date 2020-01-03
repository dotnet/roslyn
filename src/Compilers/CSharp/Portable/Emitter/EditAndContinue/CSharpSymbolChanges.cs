// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected override ISymbolInternal GetISymbolInternalOrNull(ISymbol symbol)
        {
            return (symbol as Symbols.PublicModel.Symbol)?.UnderlyingSymbol;
        }
    }
}
