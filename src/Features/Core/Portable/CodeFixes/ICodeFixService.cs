// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface ICodeFixService
    {
        Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan textSpan, bool includeSuppressionFixes, CancellationToken cancellationToken);
        Task<CodeFixCollection> GetDocumentFixAllForIdInSpanAsync(Document document, TextSpan textSpan, string diagnosticId, CancellationToken cancellationToken);
        Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(Document document, string diagnosticId, IProgressTracker progressTracker, CancellationToken cancellationToken);
        CodeFixProvider GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
        Task<FirstDiagnosticResult> GetMostSevereFixableDiagnosticAsync(Document document, TextSpan range, CancellationToken cancellationToken);
    }
}
