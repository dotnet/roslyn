// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface ICodeFixService
    {
        Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan textSpan, bool includeSuppressionFixes, CodeActionRequestPriority priority, bool isBlocking, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken);
        Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(Document document, TextSpan textSpan, string diagnosticId, CancellationToken cancellationToken);
        Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(Document document, string diagnosticId, IProgressTracker progressTracker, CancellationToken cancellationToken);
        CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
        Task<FirstDiagnosticResult> GetMostSevereFixableDiagnosticAsync(Document document, TextSpan range, CancellationToken cancellationToken);
    }

    internal static class ICodeFixServiceExtensions
    {
        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, Document document, TextSpan range, bool includeSuppressionFixes, CancellationToken cancellationToken)
            => service.GetFixesAsync(document, range, includeSuppressionFixes, isBlocking: false, cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, Document document, TextSpan range, bool includeSuppressionFixes, bool isBlocking, CancellationToken cancellationToken)
            => service.GetFixesAsync(document, range, includeSuppressionFixes, CodeActionRequestPriority.None, isBlocking, addOperationScope: _ => null, cancellationToken);
    }
}
