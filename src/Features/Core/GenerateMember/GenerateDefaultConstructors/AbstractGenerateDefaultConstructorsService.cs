// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService> : IGenerateDefaultConstructorsService
        where TService : AbstractGenerateDefaultConstructorsService<TService>
    {
        protected AbstractGenerateDefaultConstructorsService()
        {
        }

        protected abstract bool TryInitializeState(SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken, out SyntaxNode baseTypeNode, out INamedTypeSymbol classType);

        public async Task<IGenerateDefaultConstructorsResult> GenerateDefaultConstructorsAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateDefaultConstructors, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                if (textSpan.IsEmpty)
                {
                    var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
                    if (state != null)
                    {
                        return new GenerateDefaultConstructorsResult(new CodeRefactoring(null, GetActions(document, state)));
                    }
                }

                return GenerateDefaultConstructorsResult.Failure;
            }
        }

        private IEnumerable<CodeAction> GetActions(Document document, State state)
        {
            foreach (var constructor in state.UnimplementedConstructors)
            {
                yield return new GenerateDefaultConstructorCodeAction((TService)this, document, state, constructor);
            }

            if (state.UnimplementedConstructors.Count > 1)
            {
                yield return new CodeActionAll((TService)this, document, state, state.UnimplementedConstructors);
            }
        }
    }
}
