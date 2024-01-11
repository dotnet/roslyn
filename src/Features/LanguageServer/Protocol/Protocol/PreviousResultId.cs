﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Class representing a previous result id in a workspace pull request.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#previousResultId">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
internal class PreviousResultId
{
    /// <summary>
    /// Gets or sets the URI for which the client knows a result id.
    /// </summary>
    [DataMember(Name = "uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri Uri
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of the previous result id.
    /// </summary>
    [DataMember(Name = "value")]
    public string Value
    {
        get;
        set;
    }
}