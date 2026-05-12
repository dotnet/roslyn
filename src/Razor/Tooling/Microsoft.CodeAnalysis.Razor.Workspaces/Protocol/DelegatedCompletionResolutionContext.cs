// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal record DelegatedCompletionResolutionContext(
    [property: JsonPropertyName("projectedKind")] RazorLanguageKind ProjectedKind,
    [property: JsonPropertyName("originalCompletionListData")] object? OriginalCompletionListData,
    [property: JsonPropertyName("provisionalTextEdit")] TextEdit? ProvisionalTextEdit) : ICompletionResolveContext;
