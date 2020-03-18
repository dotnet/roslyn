﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
