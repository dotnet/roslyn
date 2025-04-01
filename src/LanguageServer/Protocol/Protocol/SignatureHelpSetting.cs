// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to the `textDocument/signatureHelp` request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class SignatureHelpSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// Client capabilities specific to <see cref="Protocol.SignatureInformation"/>.
    /// </summary>
    [JsonPropertyName("signatureInformation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SignatureInformationSetting? SignatureInformation
    {
        get;
        set;
    }

    /// <summary>
    /// The client supports sending additional context information for
    /// the <c>textDocument/signatureHelp</c> request.
    /// <para>
    /// A client that opts into this will also support <see cref="SignatureHelpOptions.RetriggerCharacters"/>.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("contextSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ContextSupport
    {
        get;
        set;
    }
}
