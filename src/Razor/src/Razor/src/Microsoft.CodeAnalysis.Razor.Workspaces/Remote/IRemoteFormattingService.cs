// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteFormattingService
{
    ValueTask<bool> SetFormattingLogDirectoryAsync(string? logDirectory, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<TextChange>> GetDocumentFormattingEditsAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<TextChange> htmlChanges,
        RazorFormattingOptions options,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<TextChange>> GetRangeFormattingEditsAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<TextChange> htmlChanges,
        LinePositionSpan linePositionSpan,
        RazorFormattingOptions options,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<TextChange>> GetOnTypeFormattingEditsAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<TextChange> htmlChanges,
        LinePosition linePosition,
        string triggerCharacter,
        RazorFormattingOptions options,
        CancellationToken cancellationToken);

    ValueTask<TriggerKind> GetOnTypeFormattingTriggerKindAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        LinePosition linePosition,
        string triggerCharacter,
        CancellationToken cancellationToken);

    internal enum TriggerKind
    {
        Invalid,
        ValidHtml,
        ValidCSharp,
    }
}
