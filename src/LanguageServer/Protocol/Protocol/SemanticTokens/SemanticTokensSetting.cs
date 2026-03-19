// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities for semantic tokens.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class SemanticTokensSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// Which requests the client supports and might send to the server
    /// depending on the server's capability.
    /// </summary>
    /// <remarks>
    /// Please note that clients might not
    /// show semantic tokens or degrade some of the user experience if a range
    /// or full request is advertised by the client but not provided by the
    /// server. If for example the client capability <see cref="SemanticTokensRequestsSetting.Full"/>
    /// and <see cref="SemanticTokensRequestsSetting.Range"/> are both set to true
    /// but the server only provides a  range provider the client might not
    /// render a minimap correctly or might even decide to not show any
    /// semantic tokens at all.
    /// </remarks>
    [JsonPropertyName("requests")]
    [JsonRequired]
    public SemanticTokensRequestsSetting Requests { get; set; }

    /// <summary>
    /// Gets or sets an array of token types supported by the client for encoding
    /// semantic tokens.
    /// </summary>
    [JsonPropertyName("tokenTypes")]
    [JsonRequired]
    public string[] TokenTypes { get; set; }

    /// <summary>
    /// Gets or sets an array of token modifiers supported by the client for encoding
    /// semantic tokens.
    /// </summary>
    [JsonPropertyName("tokenModifiers")]
    [JsonRequired]
    public string[] TokenModifiers { get; set; }

    /// <summary>
    /// Gets or sets an array of formats the clients supports.
    /// </summary>
    [JsonPropertyName("formats")]
    [JsonRequired]
    public SemanticTokenFormat[] Formats { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client supports tokens that can overlap each other.
    /// </summary>
    [JsonPropertyName("overlappingTokenSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool OverlappingTokenSupport { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client supports tokens that can span multiple lines.
    /// </summary>
    [JsonPropertyName("multilineTokenSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool MultilineTokenSupport { get; set; }

    /// <summary>
    /// Whether the client allows the server to actively cancel a
    /// semantic token request, e.g. supports returning
    /// ErrorCodes.ServerCancelled.
    /// <para>
    /// If a server does the client needs to retrigger the request.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("serverCancelSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ServerCancelSupport { get; init; }

    /// <summary>
    /// Whether the client uses semantic tokens to augment existing
    /// syntax tokens. If set to <see langword="true"/> client side created syntax
    /// tokens and semantic tokens are both used for colorization. If
    /// set to <see langword="false"/> the client only uses the returned semantic tokens
    /// for colorization.
    /// <para>
    /// If the value is <see langword="null"/> then the client behavior is not
    /// specified.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("augmentsSyntaxTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AugmentsSyntaxTokens { get; init; }
}
