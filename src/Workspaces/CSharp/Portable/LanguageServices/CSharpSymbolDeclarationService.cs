// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ISymbolDeclarationService), LanguageNames.CSharp), Shared]
    internal class CSharpSymbolDeclarationService : ISymbolDeclarationService
    {
        public IEnumerable<SyntaxReference> GetDeclarations(ISymbol symbol)
        {
            return symbol != null
                ? symbol.DeclaringSyntaxReferences.AsEnumerable()
                : SpecializedCollections.EmptyEnumerable<SyntaxReference>();
        }
    }
}
