// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppression
{
    internal partial class CSharpRemoveUnnecessarySuppressionCodeFixProvider
    {
        private class CSharpRemoveUnnecessarySuppressionFixAllProvider : DocumentBasedFixAllProvider
        {
            public CSharpRemoveUnnecessarySuppressionFixAllProvider()
            {
            }

            protected override string CodeActionTitle
                => CSharpFeaturesResources.Remove_suppression_operators;

            protected override async Task<SyntaxNode?> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                var cancellationToken = fixAllContext.CancellationToken;
                var newDoc = await FixAllAsync(
                    document, diagnostics,
                    fixAllContext.CodeActionEquivalenceKey == NegateExpression,
                    cancellationToken).ConfigureAwait(false);
                return await newDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
