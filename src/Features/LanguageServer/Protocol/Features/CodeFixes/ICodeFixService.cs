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
        IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(Document document, TextSpan textSpan, CodeActionRequestPriority priority, CodeActionOptions options, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken);
        Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(Document document, TextSpan textSpan, string diagnosticId, CodeActionOptions options, CancellationToken cancellationToken);
        CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
        Task<FirstDiagnosticResult> GetMostSevereFixableDiagnosticAsync(Document document, TextSpan range, CodeActionOptions options, CancellationToken cancellationToken);
    }

    internal static class ICodeFixServiceExtensions
    {
        public static IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(this ICodeFixService service, Document document, TextSpan range, CodeActionOptions options, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, range, CodeActionRequestPriority.None, options, addOperationScope: _ => null, cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, Document document, TextSpan range, CodeActionOptions options, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, range, options, cancellationToken).ToImmutableArrayAsync(cancellationToken);

        public static Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(this ICodeFixService service, Document document, TextSpan textSpan, CodeActionRequestPriority priority, CodeActionOptions options, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken)
            => service.StreamFixesAsync(document, textSpan, priority, options, addOperationScope, cancellationToken).ToImmutableArrayAsync(cancellationToken);
    }
}
