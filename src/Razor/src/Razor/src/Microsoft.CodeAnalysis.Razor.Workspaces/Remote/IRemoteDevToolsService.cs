// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteDevToolsService
{
    ValueTask<string> GetCSharpDocumentTextAsync(RazorSolutionWrapper solutionInfo, DocumentId razorDocumentId, CancellationToken cancellationToken);

    ValueTask<string> GetHtmlDocumentTextAsync(RazorSolutionWrapper solutionInfo, DocumentId razorDocumentId, CancellationToken cancellationToken);

    ValueTask<string> GetFormattingDocumentTextAsync(RazorSolutionWrapper solutionInfo, DocumentId razorDocumentId, CancellationToken cancellationToken);

    ValueTask<string> GetTagHelpersJsonAsync(RazorSolutionWrapper solutionInfo, DocumentId razorDocumentId, TagHelpersKind kind, CancellationToken cancellationToken);

    ValueTask<SyntaxVisualizerTree?> GetRazorSyntaxTreeAsync(RazorSolutionWrapper solutionInfo, DocumentId razorDocumentId, CancellationToken cancellationToken);
}
