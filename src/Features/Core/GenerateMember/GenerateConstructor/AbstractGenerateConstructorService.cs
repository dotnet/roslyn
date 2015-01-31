// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax> :
        IGenerateConstructorService
        where TService : AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
        where TArgumentSyntax : SyntaxNode
        where TAttributeArgumentSyntax : SyntaxNode
    {
        protected AbstractGenerateConstructorService()
        {
        }

        protected abstract bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);

        protected abstract bool TryInitializeConstructorInitializerGeneration(SemanticDocument document, SyntaxNode constructorInitializer, CancellationToken cancellationToken, out SyntaxToken token, out IList<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeSimpleNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out IList<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeSimpleAttributeNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out IList<TArgumentSyntax> arguments, out IList<TAttributeArgumentSyntax> attributeArguments, out INamedTypeSymbol typeToGenerateIn);

        protected abstract IList<string> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TArgumentSyntax> arguments, IList<string> reservedNames = null);
        protected virtual IList<string> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TAttributeArgumentSyntax> arguments, IList<string> reservedNames = null) { return null; }
        protected abstract string GenerateNameForArgument(SemanticModel semanticModel, TArgumentSyntax argument);
        protected virtual string GenerateNameForArgument(SemanticModel semanticModel, TAttributeArgumentSyntax argument) { return null; }
        protected abstract RefKind GetRefKind(TArgumentSyntax argument);
        protected abstract bool IsNamedArgument(TArgumentSyntax argument);
        protected abstract ITypeSymbol GetArgumentType(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);
        protected virtual ITypeSymbol GetAttributeArgumentType(SemanticModel semanticModel, TAttributeArgumentSyntax argument, CancellationToken cancellationToken) { return null; }

        public async Task<IEnumerable<CodeAction>> GenerateConstructorAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateConstructor, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeAction>();
                }

                return GetActions(document, state);
            }
        }

        private IEnumerable<CodeAction> GetActions(Document document, State state)
        {
            yield return new GenerateConstructorCodeAction((TService)this, document, state);
        }
    }
}
