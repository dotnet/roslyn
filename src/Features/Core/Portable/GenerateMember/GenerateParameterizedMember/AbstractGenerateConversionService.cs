// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
        AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>, IGenerateConversionService
        where TService : AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
        where TSimpleNameSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
    {
        protected abstract bool IsImplicitConversionGeneration(SyntaxNode node);
        protected abstract bool IsExplicitConversionGeneration(SyntaxNode node);
        protected abstract bool TryInitializeImplicitConversionState(SemanticDocument document, SyntaxNode expression, ISet<TypeKind> classInterfaceModuleStructTypes, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IMethodSymbol methodSymbol, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeExplicitConversionState(SemanticDocument document, SyntaxNode expression, ISet<TypeKind> classInterfaceModuleStructTypes, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IMethodSymbol methodSymbol, out INamedTypeSymbol typeToGenerateIn);

        public async Task<IEnumerable<CodeAction>> GenerateConversionAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateMethod, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var state = await State.GenerateConversionStateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeAction>();
                }

                return GetActions(document, state, cancellationToken);
            }
        }
    }
}
