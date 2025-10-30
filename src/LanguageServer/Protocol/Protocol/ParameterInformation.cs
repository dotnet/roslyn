// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a parameter of a callable-signature. A parameter can 
/// have a label and documentation.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#parameterInformation">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
[JsonConverter(typeof(ParameterInformationConverter))]
internal sealed class ParameterInformation
{
    /// <summary>
    /// The label of this parameter information.
    /// <para>
    /// Either a string or an inclusive start and exclusive end offsets within
    /// its containing signature label (see <see cref="SignatureInformation.Label"/>).
    /// The offsets are based on a UTF-16 string representation, like <see cref="Position"/> and
    /// <see cref="Range"/>.
    /// </para>
    /// <para>
    /// Note*: a label of type <see langword="string"/> should be a substring of its containing
    /// signature label. Its intended use case is to highlight the parameter
    /// label part in the <see cref="SignatureInformation.Label"/>.
    /// </para>
    /// </summary>
    [JsonPropertyName("label")]
    public SumType<string, Tuple<int, int>> Label
    {
        get;
        set;
    }

    /// <summary>
    /// Human-readable documentation of the parameter. Will be shown in the UI but can be omitted.
    /// </summary>
    [JsonPropertyName("documentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<string, MarkupContent>? Documentation
    {
        get;
        set;
    }
}
