// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

//
// Summary:
//     Class representing the parameters sent from the client to the server for the
//     roslyn/simplifyMethod request.
internal record SimplifyMethodParams : ITextDocumentParams
{
    //
    // Summary:
    //     Gets or sets the value which identifies the document where the text edit will be placed.
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; set; }

    //
    // Summary:
    //     Gets or sets the value which identifies the text edit to be simplified.
    [JsonPropertyName("textEdit")]
    public required TextEdit TextEdit { get; set; }
}
