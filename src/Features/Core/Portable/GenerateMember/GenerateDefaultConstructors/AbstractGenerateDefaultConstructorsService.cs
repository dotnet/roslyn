// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService> : IGenerateDefaultConstructorsService
        where TService : AbstractGenerateDefaultConstructorsService<TService>
    {
        protected AbstractGenerateDefaultConstructorsService()
        {
        }

        protected abstract bool TryInitializeState(SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken, out INamedTypeSymbol classType);

        public async Task<ImmutableArray<CodeAction>> GenerateDefaultConstructorsAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateDefaultConstructors, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var result = ArrayBuilder<CodeAction>.GetInstance();
                if (textSpan.IsEmpty)
                {
                    var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
                    if (state != null)
                    {
                        foreach (var constructor in state.UnimplementedConstructors)
                        {
                            result.Add(new GenerateDefaultConstructorCodeAction((TService)this, document, state, constructor));
                        }

                        if (state.UnimplementedConstructors.Length > 1)
                        {
                            result.Add(new CodeActionAll((TService)this, document, state, state.UnimplementedConstructors));
                        }
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
