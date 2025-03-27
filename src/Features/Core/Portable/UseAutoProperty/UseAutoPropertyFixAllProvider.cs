// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty;

internal abstract partial class AbstractUseAutoPropertyCodeFixProvider<
    TProvider,
    TTypeDeclarationSyntax,
    TPropertyDeclaration,
    TVariableDeclarator,
    TConstructorDeclaration,
    TExpression>
{
    private sealed class UseAutoPropertyFixAllProvider(TProvider provider) : FixAllProvider
    {
        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => DefaultFixAllProviderHelpers.GetFixAsync(
                fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);

        private async Task<Solution?> FixAllContextsHelperAsync(FixAllContext originalContext, ImmutableArray<FixAllContext> contexts)
        {
            var cancellationToken = originalContext.CancellationToken;

            // Very slow approach, but the only way we know how to do this correctly and without colliding edits. We
            // effectively apply each fix one at a time, moving the solution forward each time.  As we process each
            // diagnostic, we attempt to re-recover the field/property it was referring to in the original solution to
            // the current solution.
            var originalSolution = originalContext.Solution;
            var currentSolution = originalSolution;

            foreach (var currentContext in contexts)
            {
                var documentToDiagnostics = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(currentContext).ConfigureAwait(false);
                foreach (var (_, diagnostics) in documentToDiagnostics)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        currentSolution = await provider.ProcessResultAsync(
                            originalSolution, currentSolution, diagnostic, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return currentSolution;
        }
    }
}
