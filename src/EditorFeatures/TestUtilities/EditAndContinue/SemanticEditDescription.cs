// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public readonly IEnumerable<KeyValuePair<TextSpan, TextSpan>> SyntaxMap;
        public readonly bool PreserveLocalVariables;

        public SemanticEditDescription(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, IEnumerable<KeyValuePair<TextSpan, TextSpan>> syntaxMap, bool preserveLocalVariables)
        {
            this.Kind = kind;
            this.SymbolProvider = symbolProvider;
            this.SyntaxMap = syntaxMap;
            this.PreserveLocalVariables = preserveLocalVariables;
        }
    }
}
