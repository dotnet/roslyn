// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal interface IPythiaSignatureHelpProviderImplementation
{
    Task<(ImmutableArray<PythiaSignatureHelpItemWrapper> items, int? selectedItemIndex)> GetMethodGroupItemsAndSelectionAsync(ImmutableArray<IMethodSymbol> accessibleMethods, Document document, InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, SymbolInfo currentSymbol, CancellationToken cancellationToken);
}
