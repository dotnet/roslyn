// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors;

internal abstract partial class AbstractGenerateDefaultConstructorsService<TService> : IGenerateDefaultConstructorsService
    where TService : AbstractGenerateDefaultConstructorsService<TService>
{
    protected abstract bool TryInitializeState(
        SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken,
        [NotNullWhen(true)] out INamedTypeSymbol? classType);

    public async Task<ImmutableArray<CodeAction>> GenerateDefaultConstructorsAsync(
        Document document,
        TextSpan textSpan,
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
                    // If the user only has an implicit no-arg constructor, and we're adding a constructor with args,
                    // then the compiler will not emit the implicit no-arg constructor anymore.  So we need to include
                    // that member to ensure binary compat when generating members.
                    var unimplementedDefaultConstructor = state.UnimplementedConstructors.FirstOrDefault(
                        m => m.Parameters.Length == 0);

                    foreach (var constructor in state.UnimplementedConstructors)
                    {
                        Contract.ThrowIfNull(state.ClassType);

                        result.Add(new GenerateDefaultConstructorsCodeAction(
                            document, state,
                            string.Format(CodeFixesResources.Generate_constructor_0_1,
                                state.ClassType.Name,
                                string.Join(", ", constructor.Parameters.Select(p => p.Name))),
                            unimplementedDefaultConstructor == null || unimplementedDefaultConstructor == constructor
                                ? [constructor]
                                : [unimplementedDefaultConstructor, constructor]));
                    }

                    if (state.UnimplementedConstructors.Length > 1)
                        result.Add(new GenerateDefaultConstructorsCodeAction(document, state, CodeFixesResources.Generate_all, state.UnimplementedConstructors));
                }
            }

            return result.ToImmutableAndClear();
        }
    }
}
