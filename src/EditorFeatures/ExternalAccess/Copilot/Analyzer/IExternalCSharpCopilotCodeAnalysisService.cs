// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal interface IExternalCSharpCopilotCodeAnalysisService
{
    // mirror the ICopilotCodeAnalysisService interface
    Task<bool> IsAvailableAsync(CancellationToken cancellation);
    Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken);
    Task<ImmutableArray<Diagnostic>> AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken);
    Task<ImmutableArray<Diagnostic>> GetCachedDiagnosticsAsync(Document document, string promptTitle, CancellationToken cancellationToken);
    Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken);
    Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken);
    Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken);
}
