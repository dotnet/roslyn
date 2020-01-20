// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia
{
    /// <summary>
    /// Ensure this is ordered before the regular invocation signature help provider.
    /// We must replace the entire list of results, including both Pythia and non-Pythia recommendations.
    /// </summary>
    [ExportSignatureHelpProvider("PythiaSignatureHelpProvider", LanguageNames.CSharp), Shared]
    [ExtensionOrder(Before = "InvocationExpressionSignatureHelpProvider")]
    internal sealed class PythiaSignatureHelpProvider : InvocationExpressionSignatureHelpProviderBase
    {
        private readonly IPythiaSignatureHelpProviderImplementation _implementation;

        [ImportingConstructor]
        public PythiaSignatureHelpProvider(IPythiaSignatureHelpProviderImplementation implementation)
        {
            _implementation = implementation;
        }

        internal async override Task<(ImmutableArray<SignatureHelpItem> items, int? selectedItemIndex)> GetMethodGroupItemsAndSelectionAsync(
            ImmutableArray<IMethodSymbol> accessibleMethods,
            Document document,
            InvocationExpressionSyntax invocationExpression,
            SemanticModel semanticModel,
            SymbolInfo currentSymbol,
            CancellationToken cancellationToken)
        {
            var (items, selectedItemIndex) = await _implementation.GetMethodGroupItemsAndSelectionAsync(accessibleMethods, document, invocationExpression, semanticModel, currentSymbol, cancellationToken).ConfigureAwait(false);
            return (items.SelectAsArray(item => item.UnderlyingObject), selectedItemIndex);
        }
    }
}
