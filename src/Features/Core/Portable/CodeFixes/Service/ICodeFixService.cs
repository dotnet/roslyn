// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal interface ICodeFixService
{
    IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(TextDocument document, TextSpan textSpan, CodeActionRequestPriority? priority, CancellationToken cancellationToken);

    /// <summary>
    /// Similar to <see cref="StreamFixesAsync"/> except that instead of streaming all results, this ends with the
    /// first.  This will also attempt to return a fix for an error first, but will fall back to any fix if that
    /// does not succeed.
    /// </summary>
    Task<CodeFixCollection?> GetMostSevereFixAsync(TextDocument document, TextSpan range, CodeActionRequestPriority? priority, CancellationToken cancellationToken);

    Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(Document document, TextSpan? textSpan, string diagnosticId, DiagnosticSeverity severity, CancellationToken cancellationToken);
    Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(Document document, TextSpan? textSpan, string diagnosticId, DiagnosticSeverity severity, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken);

    CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
}

internal static class ICodeFixServiceExtensions
{
    public static IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(this ICodeFixService service, TextDocument document, TextSpan range, CancellationToken cancellationToken)
        => service.StreamFixesAsync(document, range, priority: null, cancellationToken);

    public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, TextDocument document, TextSpan range, CancellationToken cancellationToken)
        => service.StreamFixesAsync(document, range, cancellationToken).ToImmutableArrayAsync(cancellationToken);

    public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, TextDocument document, TextSpan textSpan, CodeActionRequestPriority? priority, CancellationToken cancellationToken)
        => service.StreamFixesAsync(document, textSpan, priority, cancellationToken).ToImmutableArrayAsync(cancellationToken);
}
