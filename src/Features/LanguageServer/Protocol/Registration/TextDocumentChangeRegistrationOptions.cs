// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Describe options to be used when registering for text document change events.
/// </summary>
[DataContract]
internal class TextDocumentChangeRegistrationOptions : TextDocumentRegistrationOptions
{
    /// <summary>
    /// How documents are synced to the server. See <see cref="TextDocumentSyncKind.Full"/>
    /// and <see cref="TextDocumentSyncKind.Incremental"/>.
    /// </summary>
    [DataMember(Name = "syncKind")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public TextDocumentSyncKind SyncKind { get; set; }
}
