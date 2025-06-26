// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia;

/// <summary>
/// Ensure this is ordered before the regular invocation signature help provider.
/// We must replace the entire list of results, including both Pythia and non-Pythia recommendations.
/// </summary>
[ExportSignatureHelpProvider(nameof(PythiaSignatureHelpProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(Before = nameof(InvocationExpressionSignatureHelpProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PythiaSignatureHelpProvider(Lazy<IPythiaSignatureHelpProviderImplementation> implementation) : InvocationExpressionSignatureHelpProviderBase
{
    private readonly Lazy<IPythiaSignatureHelpProviderImplementation> _lazyImplementation = implementation;

    internal override async Task<(ImmutableArray<SignatureHelpItem> items, int? selectedItemIndex)> GetMethodGroupItemsAndSelectionAsync(
        ImmutableArray<IMethodSymbol> accessibleMethods,
        Document document,
        InvocationExpressionSyntax invocationExpression,
        SemanticModel semanticModel,
        SymbolInfo symbolInfo,
        IMethodSymbol? currentSymbol,
        CancellationToken cancellationToken)
    {
        var (items, selectedItemIndex) = await _lazyImplementation.Value.GetMethodGroupItemsAndSelectionAsync(accessibleMethods, document, invocationExpression, semanticModel, symbolInfo, cancellationToken).ConfigureAwait(false);
        return (items.SelectAsArray(item => item.UnderlyingObject), selectedItemIndex);
    }
}
