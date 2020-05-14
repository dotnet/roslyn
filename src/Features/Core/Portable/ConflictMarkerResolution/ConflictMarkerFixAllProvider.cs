// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConflictMarkerResolution
{
    internal abstract partial class AbstractResolveConflictMarkerCodeFixProvider
    {
        private class ConflictMarkerFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly AbstractResolveConflictMarkerCodeFixProvider _codeFixProvider;

            public ConflictMarkerFixAllProvider(AbstractResolveConflictMarkerCodeFixProvider codeFixProvider)
                => _codeFixProvider = codeFixProvider;

            protected override string CodeActionTitle
                => FeaturesResources.Resolve_conflict_markers;

            protected override Task<SyntaxNode?> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
                => _codeFixProvider.FixAllAsync(document, diagnostics, fixAllContext.CodeActionEquivalenceKey, fixAllContext.CancellationToken).AsNullable();
        }
    }
}
