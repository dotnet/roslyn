// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// LSP Params for textDocument/mapCode calls.
/// </summary>
[DataContract]
internal record MapCodeParams(
    // Set of code blocks, associated with documents and regions, to map.
    [property: DataMember(Name = "mappings")] MapCodeMapping[] Mappings,
    // Changes that should be applied to the workspace by the mapper before performing the mapping operation.
    [property: DataMember(Name = "updates")] WorkspaceEdit? Updates
);

[DataContract]
internal record MapCodeMapping
(
    // Gets or sets identifier for the document the contents are supposed to be mapped into.
    [property: DataMember(Name = "textDocument")] TextDocumentIdentifier? TextDocument,
    // Gets or sets strings of code/text to map into TextDocument.
    [property: DataMember(Name = "contents")] string[] Contents,
    // Prioritized Locations to be used when applying heuristics. For example, cursor location,
    // related classes (in other documents), viewport, etc. Earlier items should be considered
    // higher priority.
    [property: DataMember(Name = "focusLocations")] Location[][]? FocusLocations
);
