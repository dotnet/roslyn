// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable 

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptDefinitionItemBase : DefinitionItem
    {
        protected VSTypeScriptDefinitionItemBase(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts)
            : base(tags,
                  displayParts,
                  ImmutableArray<TaggedText>.Empty,
                  originationParts: default,
                  sourceSpans: default,
                  properties: null,
                  displayIfNoReferences: true)
        {
        }
    }
}
