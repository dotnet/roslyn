// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.CodeFixes;

namespace Microsoft.CodeAnalysis.ConflictMarkerResolution
{
    internal abstract partial class AbstractResolveConflictMarkerCodeFixProvider
    {
        private class ConflictMarkerFixAllProvider : AbstractConcurrentFixAllProvider
        {
            private readonly AbstractResolveConflictMarkerCodeFixProvider _codeFixProvider;

            public ConflictMarkerFixAllProvider(AbstractResolveConflictMarkerCodeFixProvider codeFixProvider)
                => _codeFixProvider = codeFixProvider;

            protected override Task<Document> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> filteredDiagnostics)
                => _codeFixProvider.FixAllAsync(document, filteredDiagnostics, fixAllContext.CodeActionEquivalenceKey, fixAllContext.CancellationToken);
        }
    }
}
