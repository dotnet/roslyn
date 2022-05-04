// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService> : IGenerateDefaultConstructorsService
        where TService : AbstractGenerateDefaultConstructorsService<TService>
    {
        protected AbstractGenerateDefaultConstructorsService()
        {
        }

        protected abstract bool TryInitializeState(
            SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken,
            [NotNullWhen(true)] out INamedTypeSymbol? classType);

        public async Task<ImmutableArray<CodeAction>> GenerateDefaultConstructorsAsync(
            Document document,
            TextSpan textSpan,
            CodeAndImportGenerationOptionsProvider fallbackOptions,
            bool forRefactoring,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateDefaultConstructors, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                using var _ = ArrayBuilder<CodeAction>.GetInstance(out var result);
                if (textSpan.IsEmpty)
                {
                    var state = State.Generate((TService)this, semanticDocument, textSpan, forRefactoring, cancellationToken);
                    if (state != null)
                    {
                        foreach (var constructor in state.UnimplementedConstructors)
                            result.Add(new GenerateDefaultConstructorCodeAction(document, state, constructor, fallbackOptions));

                        if (state.UnimplementedConstructors.Length > 1)
                            result.Add(new CodeActionAll(document, state, state.UnimplementedConstructors, fallbackOptions));
                    }
                }

                return result.ToImmutable();
            }
        }
    }
}
