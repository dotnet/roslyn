// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.CodeAnalysis.Razor.Protocol.Completion;

/// <summary>
/// The result of a phase-1 completion call, carrying both the completion list and
/// a flag indicating whether a follow-up phase-2 call is needed for HTML-dependent
/// providers.
/// </summary>
/// <remarks>
/// Static helpers are on <see cref="CompletionResults"/> to avoid a self-referential
/// value type (struct containing <c>RemoteResponse&lt;CompletionResult&gt;</c>) which
/// causes a <see cref="System.TypeLoadException"/> on .NET Framework.
/// </remarks>
internal record struct CompletionResult(
    [property: JsonPropertyName("completionList")] RazorVSInternalCompletionList? CompletionList,
    [property: JsonPropertyName("needsHtmlDependentPhase")] bool NeedsHtmlDependentPhase);

/// <summary>
/// Factory helpers for <see cref="CompletionResult"/> responses. Separated from the
/// struct itself to avoid a self-referential generic value type layout cycle on .NET Framework.
/// </summary>
internal static class CompletionResults
{
    public static readonly RemoteResponse<CompletionResult> CallHtml
        = new(StopHandling: false, new CompletionResult(null, NeedsHtmlDependentPhase: false));

    public static RemoteResponse<CompletionResult> Create(RazorVSInternalCompletionList? completionList, bool needsHtmlDependentPhase)
        => new(StopHandling: false, new CompletionResult(completionList, needsHtmlDependentPhase));
}
