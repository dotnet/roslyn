// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotCodeAnalysisService : IWorkspaceService
{
    bool IsRefineOptionEnabled(Document document);
    bool IsCodeAnalysisOptionEnabled(Document document);

    Task<bool> IsAvailableAsync(Document document, CancellationToken cancellationToken);

    Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken);

    Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken);

    Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, ImmutableArray<string> promptTitles, CancellationToken cancellationToken);

    Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken);
}
