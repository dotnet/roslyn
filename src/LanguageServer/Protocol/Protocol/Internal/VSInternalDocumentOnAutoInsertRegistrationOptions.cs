﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class representing the registration options for on auto insert support.
/// </summary>
internal class VSInternalDocumentOnAutoInsertRegistrationOptions : VSInternalDocumentOnAutoInsertOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// Gets or sets the document filters for this registration option.
    /// </summary>
    [JsonPropertyName("documentSelector")]
    public DocumentFilter[]? DocumentSelector
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the id used to register the request.  The id can be used to deregister the request again.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id
    {
        get;
        set;
    }
}