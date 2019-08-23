// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ISymbolDeclarationService), LanguageNames.CSharp), Shared]
    internal class CSharpSymbolDeclarationService : ISymbolDeclarationService
    {
#if CODE_STYLE
        public static readonly ISymbolDeclarationService Instance = new CSharpSymbolDeclarationService();   
#endif

        [ImportingConstructor]
        public CSharpSymbolDeclarationService()
        {
        }

        public ImmutableArray<SyntaxReference> GetDeclarations(ISymbol symbol)
            => symbol != null
                ? symbol.DeclaringSyntaxReferences
                : ImmutableArray<SyntaxReference>.Empty;
    }
}
