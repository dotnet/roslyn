// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;
using Microsoft.CodeAnalysis.GoToBase;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GoToBase;

[ExportLanguageService(typeof(IGoToBaseService), LanguageNames.CSharp), Shared]
internal sealed class CSharpGoToBaseService : AbstractGoToBaseService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpGoToBaseService()
    {
    }

    protected override async Task<IMethodSymbol?> FindNextConstructorInChainAsync(
        Solution solution, IMethodSymbol constructor, CancellationToken cancellationToken)
    {
        if (constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is not ConstructorDeclarationSyntax constructorDeclaration)
            return null;

        var document = solution.GetDocument(constructorDeclaration.SyntaxTree);
        if (document is null)
            return null;

        // this constructor must be calling an accessible no-arg constructor in the base type.
        if (constructorDeclaration.Initializer is null)
            return FindBaseNoArgConstructor(constructor);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return semanticModel.GetSymbolInfo(constructorDeclaration.Initializer, cancellationToken).GetAnySymbol() as IMethodSymbol;
    }
}
