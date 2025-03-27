// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to code actions.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class CodeActionSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// The client supports code action literals as a valid response of
    /// the <c>textDocument/codeAction</c> request.
    /// </summary>
    /// <remarks>Since LSP 3.8</remarks>
    [JsonPropertyName("codeActionLiteralSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeActionLiteralSetting? CodeActionLiteralSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Whether code action supports the <see cref="CodeAction.IsPreferred"/> property.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("isPreferredSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsPreferredSupport { get; init; }

    /// <summary>
    /// Whether code action supports the <see cref="CodeAction.Disabled"/> property.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("disabledSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DisabledSupport { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether code action supports the <see cref="CodeAction.Data"/>
    /// property which is preserved between a `textDocument/codeAction` request and a
    /// `codeAction/resolve` request.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("dataSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DataSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the client supports resolving
    /// additional code action properties via a separate <c>codeAction/resolve</c>
    /// request.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("resolveSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeActionResolveSupportSetting? ResolveSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Whether the client honors the change annotations in text edits and
    /// resource operations returned via the <see cref="CodeAction.Edit"/> property by
    /// for example presenting the workspace edit in the user interface and asking
    /// for confirmation.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("honorsChangeAnnotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HonorsChangeAnnotations { get; init; }
}
