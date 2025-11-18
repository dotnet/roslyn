// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a single signature of a callable item.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureInformation">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class SignatureInformation
{
    /// <summary>
    /// The label of this signature. Will be shown in the UI.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label
    {
        get;
        set;
    }

    /// <summary>
    /// The human-readable documentation of this signature.
    /// Will be shown in the UI but can be omitted.
    /// </summary>
    [JsonPropertyName("documentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<string, MarkupContent>? Documentation
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the parameters of this signature.
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParameterInformation[]? Parameters
    {
        get;
        set;
    }

    /// <summary>
    /// The index of the active parameter.
    /// <para>
    /// If provided, this is used in place of <see cref="SignatureHelp.ActiveParameter"/>.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("activeParameter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ActiveParameter { get; init; }
}
