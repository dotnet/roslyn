// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.RemoveConfusingSuppression
{
    internal partial class CSharpRemoveConfusingSuppressionCodeFixProvider
    {
        private class CSharpRemoveConfusingSuppressionFixAllProvider : DocumentBasedFixAllProvider
        {
            protected override Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
                => CSharpRemoveConfusingSuppressionCodeFixProvider.FixAllAsync(
                    document, diagnostics,
                    fixAllContext.CodeActionEquivalenceKey == NegateExpression,
                    fixAllContext.CancellationToken);
        }
    }
}
