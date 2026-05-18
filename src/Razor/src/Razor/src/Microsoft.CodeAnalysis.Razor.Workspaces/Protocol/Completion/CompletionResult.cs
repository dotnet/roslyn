// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.CodeAnalysis.Razor.Protocol.Completion;

/// <summary>
/// The result of a completion call, carrying the Razor/C# completion list and an optional
/// locally-computed HTML completion list.
/// </summary>
internal record struct CompletionResult(
    [property: JsonPropertyName("completionList")] RazorVSInternalCompletionList? CompletionList,
    [property: JsonPropertyName("htmlCompletionList")] RazorVSInternalCompletionList? HtmlCompletionList = null);

/// <summary>
/// Factory helpers for <see cref="CompletionResult"/> responses. Separated from the
/// struct itself to avoid a self-referential generic value type layout cycle on .NET Framework.
/// </summary>
internal static class CompletionResults
{
    public static readonly RemoteResponse<CompletionResult> CallHtml
        = new(StopHandling: false, new CompletionResult(null));

    public static RemoteResponse<CompletionResult> Create(RazorVSInternalCompletionList? completionList, RazorVSInternalCompletionList? htmlCompletionList)
        => new(StopHandling: false, new CompletionResult(completionList, htmlCompletionList));
}
