// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if MEF
    [ExportLanguageService(typeof(ISymbolDeclarationService), LanguageNames.CSharp)]
#endif
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