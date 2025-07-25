// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Structure;

internal static class BlockStructureExtensions
{
    extension(ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>>.Builder builder)
    {
        public void Add<TType, TOutliner>(
)
        where TType : SyntaxNode
        where TOutliner : AbstractSyntaxStructureProvider, new()
        {
            builder.Add(typeof(TType), [new TOutliner()]);
        }
    }
}
