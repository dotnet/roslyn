// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
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

        bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt, string baseName,
            Func<ISymbol, bool> filter, IEnumerable<string> usedNames, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueLocalName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueLocalName(
            SemanticModel semanticModel, SyntaxNode location,
            SyntaxNode containerOpt, string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken);

        SyntaxToken GenerateUniqueName(string baseName, IEnumerable<string> usedNames);
    }
}
