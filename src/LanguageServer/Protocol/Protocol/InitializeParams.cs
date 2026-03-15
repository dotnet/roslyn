// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Class which represents the parameter sent with an initialize method request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class InitializeParams : IWorkDoneProgressParams
{
    /// <summary>
    /// Gets or sets the ID of the process which launched the language server.
    /// </summary>
    [JsonPropertyName("processId")]
    public int? ProcessId
    {
        get;
        set;
    }

    /// <summary>
    /// Information about the client.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("clientInfo")]
    public ClientInfo? ClientInfo { get; set; }

    /// <summary>
    /// Gets or sets the locale the client is currently showing the user interface in.
    /// This must not necessarily be the locale of the operating system.
    ///
    /// Uses IETF language tags as the value's syntax.
    /// (See https://en.wikipedia.org/wiki/IETF_language_tag)
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("locale")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Locale
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the workspace root path.
    /// </summary>
    [JsonPropertyName("rootPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Obsolete("Deprecated in favor of RootUri")]
    public string? RootPath
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the workspace root path. Take precedence over <see cref="RootPath"/> if both are set.
    /// </summary>
    [JsonPropertyName("rootUri")]
    [Obsolete("Deprecated in favor of WorkspaceFolders")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public DocumentUri? RootDocumentUri
    {
        get;
        set;
    }

    [Obsolete("Use RootDocumentUri instead. This property will be removed in a future version.")]
    [JsonIgnore]
    public Uri RootUri
    {
        get => RootDocumentUri.GetRequiredParsedUri();
        set => RootDocumentUri = new DocumentUri(value);
    }

    /// <summary>
    /// Gets or sets the initialization options as specified by the client.
    /// </summary>
    [JsonPropertyName("initializationOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? InitializationOptions
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the capabilities supported by the client.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public ClientCapabilities Capabilities
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the initial trace setting.
    /// </summary>
    [JsonPropertyName("trace")]
    [DefaultValue(typeof(TraceValue), "off")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TraceValue Trace
    {
        get;
        set;
#pragma warning disable SA1500, SA1513 // Braces for multi-line statements should not share line, Closing brace should be followed by blank line
    } = TraceValue.Off;
#pragma warning restore SA1500, SA1513 // Braces for multi-line statements should not share line, Closing brace should be followed by blank line

    /// <summary>
    /// Workspace folders configured in the client when the server starts.
    /// <para>
    /// An empty array indicates that the client supports workspace folders but none are open,
    /// and <see langword="null"/> indicates that the client does not support workspace folders.
    /// </para>
    /// <para>
    /// Note that this is a minor change from the raw protocol, where if the property is present in JSON but <see langword="null"/>,
    /// it is equivalent to an empty array value. This distinction cannot easily be represented idiomatically in .NET,
    /// but is not important to preserve.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    [JsonPropertyName("workspaceFolders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(InitializeParamsWorkspaceFoldersConverter))]
    public WorkspaceFolder[]? WorkspaceFolders { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
