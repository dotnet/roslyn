// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[DataContract]
internal record FormatNewFileParams
{
    [DataMember(Name = "document")]
    public required TextDocumentIdentifier Document { get; set; }

    [DataMember(Name = "project")]
    public required TextDocumentIdentifier Project { get; set; }

    [DataMember(Name = "contents")]
    public required string Contents { get; set; }
}
