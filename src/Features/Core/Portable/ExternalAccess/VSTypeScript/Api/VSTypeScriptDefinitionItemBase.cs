// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    [Obsolete]
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
