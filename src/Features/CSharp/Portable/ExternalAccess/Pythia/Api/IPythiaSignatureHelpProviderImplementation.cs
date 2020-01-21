// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal interface IPythiaSignatureHelpProviderImplementation
    {
        Task<(ImmutableArray<PythiaSignatureHelpItemWrapper> items, int? selectedItemIndex)> GetMethodGroupItemsAndSelectionAsync(ImmutableArray<IMethodSymbol> accessibleMethods, Document document, InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, SymbolInfo currentSymbol, CancellationToken cancellationToken);
    }
}
