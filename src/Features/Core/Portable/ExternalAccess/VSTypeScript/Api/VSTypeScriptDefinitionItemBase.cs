// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
