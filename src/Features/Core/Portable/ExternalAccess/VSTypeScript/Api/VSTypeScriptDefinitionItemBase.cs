// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

[Obsolete]
internal abstract class VSTypeScriptDefinitionItemBase : DefinitionItem
{
    protected VSTypeScriptDefinitionItemBase(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts)
        : base(
            tags,
            displayParts,
            [],
            sourceSpans: default,
            metadataLocations: ImmutableArray<AssemblyLocation>.Empty,
            classifiedSpans: default,
            properties: null,
            displayableProperties: ImmutableDictionary<string, string>.Empty,
            displayIfNoReferences: true)
    {
    }
}
