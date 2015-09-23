// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
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

            public override async Task AddDocumentFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
            {
                var pragmaActionsBuilder = ImmutableArray.CreateBuilder<IPragmaBasedCodeAction>();
                var pragmaDiagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();

                foreach (var diagnostic in diagnostics.Where(d => d.Location.IsInSource && !d.IsSuppressed))
                {
                    var span = diagnostic.Location.SourceSpan;
                    var pragmaSuppressions = await _suppressionFixProvider.GetPragmaSuppressionsAsync(document, span, SpecializedCollections.SingletonEnumerable(diagnostic), fixAllContext.CancellationToken).ConfigureAwait(false);
                    var pragmaSuppression = pragmaSuppressions.SingleOrDefault();
                    if (pragmaSuppression != null)
                    {
                        if (fixAllContext is FixMultipleContext)
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
                    var pragmaBatchFix = PragmaBatchFixHelpers.CreateBatchPragmaFix(_suppressionFixProvider, document,
                        pragmaActionsBuilder.ToImmutable(), pragmaDiagnosticsBuilder.ToImmutable(), fixAllContext);

                    addFix(pragmaBatchFix);
                }
            }
        }
    }
}
