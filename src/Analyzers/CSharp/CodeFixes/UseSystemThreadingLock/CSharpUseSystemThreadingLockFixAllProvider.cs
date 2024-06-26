// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSystemThreadingLock;

internal sealed partial class CSharpUseSystemThreadingLockFixProvider
{
#if !CODE_STYLE
    private sealed class CSharpUseSystemThreadingLockFixAllProvider : FixAllProvider
    {
        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            return DefaultFixAllProviderHelpers.GetFixAsync(
                fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);
        }

        private static async Task<Solution?> FixAllContextsHelperAsync(FixAllContext originalContext, ImmutableArray<FixAllContext> contexts)
        {
            var cancellationToken = originalContext.CancellationToken;

            var solutionEditor = new SolutionEditor(originalContext.Solution);

            foreach (var currentContext in contexts)
            {
                var documentToDiagnostics = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(currentContext).ConfigureAwait(false);
                foreach (var (document, diagnostics) in documentToDiagnostics)
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
                    {
                        if (diagnostic.Location.FindNode(cancellationToken) is not VariableDeclaratorSyntax declarator)
                            continue;

                        await UseSystemThreadingLockAsync(
                            solutionEditor, document, semanticModel, declarator, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return solutionEditor.GetChangedSolution();
        }
    }
#endif
}
