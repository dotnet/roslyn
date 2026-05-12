// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteInlineCompletionService
{
    ValueTask<InlineCompletionRequestInfo?> GetInlineCompletionInfoAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        LinePosition position,
        CancellationToken cancellationToken);

    ValueTask<FormattedInlineCompletionInfo?> FormatInlineCompletionAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        RazorFormattingOptions options,
        LinePositionSpan span,
        string text,
        CancellationToken cancellationToken);
}

[DataContract]
internal record struct InlineCompletionRequestInfo(
    [property: DataMember(Order = 0)] Uri GeneratedDocumentUri,
    [property: DataMember(Order = 1)] LinePosition Position);

[DataContract]
internal record struct FormattedInlineCompletionInfo(
    [property: DataMember(Order = 0)] LinePositionSpan Span,
    [property: DataMember(Order = 1)] string FormattedText);
