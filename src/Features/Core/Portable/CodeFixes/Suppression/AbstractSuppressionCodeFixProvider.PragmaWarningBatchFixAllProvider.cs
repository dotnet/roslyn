// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        /// <summary>
        /// Batch fixer for pragma suppress code action.
        /// </summary>
        internal sealed class PragmaWarningBatchFixAllProvider : BatchFixAllProvider
        {
            private readonly AbstractSuppressionCodeFixProvider _suppressionFixProvider;

            public PragmaWarningBatchFixAllProvider(AbstractSuppressionCodeFixProvider suppressionFixProvider)
            {
                _suppressionFixProvider = suppressionFixProvider;
            }

            protected override async Task AddDocumentFixesAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics,
                ConcurrentBag<(Diagnostic diagnostic, CodeAction action)> fixes,
                FixAllState fixAllState, CancellationToken cancellationToken)
            {
                var pragmaActionsBuilder = ArrayBuilder<IPragmaBasedCodeAction>.GetInstance();
                var pragmaDiagnosticsBuilder = ArrayBuilder<Diagnostic>.GetInstance();

                foreach (var diagnostic in diagnostics.Where(d => d is { Location: { IsInSource: true }, IsSuppressed: false }))
                {
                    var span = diagnostic.Location.SourceSpan;
                    var pragmaSuppressions = await _suppressionFixProvider.GetPragmaSuppressionsAsync(
                        document, span, SpecializedCollections.SingletonEnumerable(diagnostic), cancellationToken).ConfigureAwait(false);
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

                    fixes.Add((diagnostic: null, pragmaBatchFix));
                }
            }
        }
    }
}
