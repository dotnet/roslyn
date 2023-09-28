// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// General text document registration options.
/// </summary>
[DataContract]
internal class TextDocumentRegistrationOptions : ITextDocumentRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [DataMember(Name = "documentSelector")]
    [JsonProperty("documentSelector", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentFilter[]? DocumentSelector { get; set; }
}
