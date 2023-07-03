// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface ICodeFixService
    {
        IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(TextDocument document, TextSpan textSpan, ICodeActionRequestPriorityProvider priorityProvider, CodeActionOptionsProvider options, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken);

        /// <summary>
        /// Similar to <see cref="StreamFixesAsync"/> except that instead of streaming all results, this ends with the
        /// first.  This will also attempt to return a fix for an error first, but will fall back to any fix if that
        /// does not succeed.
        /// </summary>
        Task<FirstFixResult> GetMostSevereFixAsync(TextDocument document, TextSpan range, ICodeActionRequestPriorityProvider priorityProvider, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);

        Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(TextDocument document, TextSpan textSpan, string diagnosticId, DiagnosticSeverity severity, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);
        Task<TDocument> ApplyCodeFixesForSpecificDiagnosticIdAsync<TDocument>(TDocument document, string diagnosticId, DiagnosticSeverity severity, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            where TDocument : TextDocument;
        CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
    }

    internal static class ICodeFixServiceExtensions
    {
        public static IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(this ICodeFixService service, TextDocument document, TextSpan range, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, range, new DefaultCodeActionRequestPriorityProvider(), fallbackOptions, addOperationScope: _ => null, cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, TextDocument document, TextSpan range, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, range, fallbackOptions, cancellationToken).ToImmutableArrayAsync(cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, TextDocument document, TextSpan textSpan, ICodeActionRequestPriorityProvider priorityProvider, CodeActionOptionsProvider fallbackOptions, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, textSpan, priorityProvider, fallbackOptions, addOperationScope, cancellationToken).ToImmutableArrayAsync(cancellationToken);

        public static Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(this ICodeFixService service, TextDocument document, TextSpan range, string diagnosticId, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => service.GetDocumentFixAllForIdInSpanAsync(document, range, diagnosticId, DiagnosticSeverity.Hidden, fallbackOptions, cancellationToken);

        public static Task<TDocument> ApplyCodeFixesForSpecificDiagnosticIdAsync<TDocument>(this ICodeFixService service, TDocument document, string diagnosticId, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken) where TDocument : TextDocument
            => service.ApplyCodeFixesForSpecificDiagnosticIdAsync(document, diagnosticId, DiagnosticSeverity.Hidden, progressTracker, fallbackOptions, cancellationToken);
    }
}
