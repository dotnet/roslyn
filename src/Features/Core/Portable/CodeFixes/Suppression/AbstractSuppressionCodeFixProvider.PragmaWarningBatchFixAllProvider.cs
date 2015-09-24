// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class PragmaWarningBatchFixAllProvider : BatchFixAllProvider
        {
            private readonly AbstractSuppressionCodeFixProvider _suppressionFixProvider;

            public PragmaWarningBatchFixAllProvider(AbstractSuppressionCodeFixProvider suppressionFixProvider)
            {
                _suppressionFixProvider = suppressionFixProvider;
            }

            public override async Task AddDocumentFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction> addFix, FixAllContext fixAllContext)
            {
                foreach (var diagnosticsForSpan in diagnostics.Where(d => d.Location.IsInSource).GroupBy(d => d.Location.SourceSpan))
                {
                    var span = diagnosticsForSpan.First().Location.SourceSpan;
                    var pragmaSuppressions = await _suppressionFixProvider.GetPragmaSuppressionsAsync(document, span, diagnosticsForSpan, fixAllContext.CancellationToken).ConfigureAwait(false);
                    foreach (var pragmaSuppression in pragmaSuppressions)
                    {
                        if (fixAllContext is FixMultipleContext)
                        {
                            addFix(pragmaSuppression.CloneForFixMultipleContext());
                        }
                        else
                        {
                            addFix(pragmaSuppression);
                        }
                    }
                }
            }
        }
    }
}
