// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages;

internal class FSharpDefinitionItem
{
    private FSharpDefinitionItem(DefinitionItem roslynDefinitionItem)
    {
        RoslynDefinitionItem = roslynDefinitionItem;
    }

    internal DefinitionItem RoslynDefinitionItem { get; }

    public static FSharpDefinitionItem Create(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, FSharpDocumentSpan sourceSpan)
        => new(
            DefinitionItem.Create(
                tags,
                displayParts,
                sourceSpans: [sourceSpan.ToRoslynDocumentSpan()],
                classifiedSpans: [],
                metadataLocations: [],
                nameDisplayParts: default,
                displayIfNoReferences: true));

    [Obsolete("Use overload that takes metadata locations")]
    public static FSharpDefinitionItem CreateNonNavigableItem(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, ImmutableArray<TaggedText> originationParts)
        => new(
            DefinitionItem.CreateNonNavigableItem(
                tags,
                displayParts,
                metadataLocations: [new AssemblyLocation() { Name = originationParts.JoinText(), Version = Versions.Null, FilePath = null }]));

    public static FSharpDefinitionItem CreateNonNavigableItem(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, ImmutableArray<(string name, Version version, string filePath)> metadataLocations)
        => new(
            DefinitionItem.CreateNonNavigableItem(
                tags,
                displayParts,
                metadataLocations: metadataLocations.SelectAsArray(l => new AssemblyLocation(l.name, l.version, l.filePath))));
}
