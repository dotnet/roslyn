// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class SimplifyTagToSelfClosingCodeActionParams
{
    [JsonPropertyName("startTagCloseAngleIndex")]
    public int StartTagCloseAngleIndex { get; set; }

    [JsonPropertyName("endTagCloseAngleIndex")]
    public int EndTagCloseAngleIndex { get; set; }
}
