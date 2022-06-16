﻿// Licensed to the .NET Foundation under one or more agreements.
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
        IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(Document document, TextSpan textSpan, CodeActionRequestPriority priority, CodeActionOptionsProvider options, bool isBlocking, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken);

        /// <summary>
        /// Similar to <see cref="StreamFixesAsync"/> except that instead of streaming all results, this ends with the
        /// first.  This will also attempt to return a fix for an error first, but will fall back to any fix if that
        /// does not succeed.
        /// </summary>
        Task<FirstFixResult> GetMostSevereFixAsync(Document document, TextSpan range, CodeActionRequestPriority priority, CodeActionOptionsProvider fallbackOptions, bool isBlocking, CancellationToken cancellationToken);

        Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(Document document, TextSpan textSpan, string diagnosticId, DiagnosticSeverity severity, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);
        Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(Document document, string diagnosticId, DiagnosticSeverity severity, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);
        CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
    }

    internal static class ICodeFixServiceExtensions
    {
        public static IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(this ICodeFixService service, Document document, TextSpan range, CodeActionOptionsProvider fallbackOptions, bool isBlocking, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, range, CodeActionRequestPriority.None, fallbackOptions, isBlocking, addOperationScope: _ => null, cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, Document document, TextSpan range, CodeActionOptionsProvider fallbackOptions, bool isBlocking, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, range, fallbackOptions, isBlocking, cancellationToken).ToImmutableArrayAsync(cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, Document document, TextSpan textSpan, CodeActionRequestPriority priority, CodeActionOptionsProvider fallbackOptions, bool isBlocking, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, textSpan, priority, fallbackOptions, isBlocking, addOperationScope, cancellationToken).ToImmutableArrayAsync(cancellationToken);

        public static Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(this ICodeFixService service, Document document, TextSpan range, string diagnosticId, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => service.GetDocumentFixAllForIdInSpanAsync(document, range, diagnosticId, DiagnosticSeverity.Hidden, fallbackOptions, cancellationToken);

        public static Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(this ICodeFixService service, Document document, string diagnosticId, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => service.ApplyCodeFixesForSpecificDiagnosticIdAsync(document, diagnosticId, DiagnosticSeverity.Hidden, progressTracker, fallbackOptions, cancellationToken);
    }
}
