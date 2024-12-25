// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
{
    /// <summary>
    /// Batch fixer for pragma suppress code action.
    /// </summary>
    internal sealed class PragmaWarningBatchFixAllProvider(AbstractSuppressionCodeFixProvider suppressionFixProvider) : AbstractSuppressionBatchFixAllProvider
    {
        private readonly AbstractSuppressionCodeFixProvider _suppressionFixProvider = suppressionFixProvider;

        protected override async Task AddDocumentFixesAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            Action<(Diagnostic diagnostic, CodeAction action)> onItemFound,
            FixAllState fixAllState, CancellationToken cancellationToken)
        {
            var pragmaActionsBuilder = ArrayBuilder<IPragmaBasedCodeAction>.GetInstance();
            var pragmaDiagnosticsBuilder = ArrayBuilder<Diagnostic>.GetInstance();

            foreach (var diagnostic in diagnostics.Where(d => d.Location.IsInSource && !d.IsSuppressed))
            {
                var span = diagnostic.Location.SourceSpan;
                var pragmaSuppressions = await _suppressionFixProvider.GetPragmaSuppressionsAsync(
                    document, span, [diagnostic], cancellationToken).ConfigureAwait(false);
                var pragmaSuppression = pragmaSuppressions.SingleOrDefault();
                if (pragmaSuppression != null)
                {
                    if (fixAllState.IsFixMultiple)
                    {
                        pragmaSuppression = pragmaSuppression.CloneForFixMultipleContext();
                    }

                    pragmaActionsBuilder.Add(pragmaSuppression);
                    pragmaDiagnosticsBuilder.Add(diagnostic);
                }
            }

            // Get the pragma batch fix.
            if (pragmaActionsBuilder.Count > 0)
            {
                var pragmaBatchFix = PragmaBatchFixHelpers.CreateBatchPragmaFix(
                    _suppressionFixProvider, document,
                    pragmaActionsBuilder.ToImmutableAndFree(),
                    pragmaDiagnosticsBuilder.ToImmutableAndFree(),
                    fixAllState, cancellationToken);

                onItemFound((diagnostic: null, pragmaBatchFix));
            }
        }
    }
}
