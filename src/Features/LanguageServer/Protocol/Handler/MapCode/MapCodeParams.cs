// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.LanguageServer.Protocol;

/// <summary>
/// LSP Params for textDocument/mapCode calls.
/// </summary>
[DataContract]
internal class MapCodeParams
{
    /// <summary>
    /// Set of code blocks, associated with documents and regions, to map.
    /// </summary>
    [DataMember(Name = "mappings")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MapCodeMapping[] Mappings { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Changes that should be applied to the workspace by the mapper before performing
    /// the mapping operation.
    /// </summary>
    [DataMember(Name = "updates")]
    public WorkspaceEdit? Updates
    {
        get;
        set;
    }
}

internal class MapCodeMapping
{
    /// <summary>
    /// Gets or sets identifier for the document the contents are supposed to be mapped into.
    /// </summary>
    [DataMember(Name = "textDocument")]
    public TextDocumentIdentifier? TextDocument { get; set; }

    /// <summary>
    /// Gets or sets strings of code/text to map into TextDocument.
    /// </summary>
    [DataMember(Name = "contents")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string[] Contents
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        get;
        set;
    }

    /// <summary>
    /// Prioritized Locations to be used when applying heuristics. For example, cursor location,
    /// related classes (in other documents), viewport, etc. Earlier items should be considered
    /// higher priority.
    /// </summary>
    [DataMember(Name = "focusLocations")]
    public Location[][]? FocusLocations
    {
        get;
        set;
    }
}
