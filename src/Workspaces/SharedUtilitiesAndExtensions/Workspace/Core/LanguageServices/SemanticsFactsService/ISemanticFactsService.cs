// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.LanguageService;

internal partial interface ISemanticFactsService : ISemanticFacts, ILanguageService
{
    bool IsExpressionContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsStatementContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsTypeContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsNamespaceContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsNamespaceDeclarationNameContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsTypeDeclarationContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsMemberDeclarationContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsGlobalStatementContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsLabelContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);
    bool IsAttributeNameContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken);

    SyntaxToken GenerateUniqueName(
        SemanticModel semanticModel, SyntaxNode location,
        SyntaxNode? container, string baseName, CancellationToken cancellationToken);

    SyntaxToken GenerateUniqueName(
        SemanticModel semanticModel, SyntaxNode location,
        SyntaxNode? container, string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken);

    SyntaxToken GenerateUniqueName(SemanticModel semanticModel, SyntaxNode location, SyntaxNode? container, string baseName,
        Func<ISymbol, bool> filter, IEnumerable<string> usedNames, CancellationToken cancellationToken);

    SyntaxToken GenerateUniqueLocalName(
        SemanticModel semanticModel, SyntaxNode location,
        SyntaxNode? container, string baseName, CancellationToken cancellationToken);

    SyntaxToken GenerateUniqueLocalName(
        SemanticModel semanticModel, SyntaxNode location,
        SyntaxNode? container, string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken);

    SyntaxToken GenerateUniqueName(string baseName, IEnumerable<string> usedNames);

    IMethodSymbol? TryGetDisposeMethod(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
}
