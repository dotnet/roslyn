// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(ISymbolDeclarationService), LanguageNames.CSharp), Shared]
internal class CSharpSymbolDeclarationService : ISymbolDeclarationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSymbolDeclarationService()
    {
    }

    public ImmutableArray<SyntaxReference> GetDeclarations(ISymbol symbol)
        => symbol != null
            ? symbol.DeclaringSyntaxReferences
            : [];
}
