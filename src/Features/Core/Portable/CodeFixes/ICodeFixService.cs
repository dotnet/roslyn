// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface ICodeFixService
    {
        Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan textSpan, bool includeSuppressionFixes, CancellationToken cancellationToken);
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        Task<CodeFixCollection> GetDocumentFixAllForIdInSpan(Document document, TextSpan textSpan, string diagnosticId, CancellationToken cancellationToken);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        CodeFixProvider GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        Task<FirstDiagnosticResult> GetMostSevereFixableDiagnostic(Document document, TextSpan range, CancellationToken cancellationToken);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    }
}
