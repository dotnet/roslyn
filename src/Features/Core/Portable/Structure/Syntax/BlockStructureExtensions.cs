// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockStructureExtensions
    {
        public static void Add<TType, TOutliner>(
            this ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>>.Builder builder)
            where TType : SyntaxNode
            where TOutliner : AbstractSyntaxStructureProvider, new()
        {
            builder.Add(typeof(TType), ImmutableArray.Create<AbstractSyntaxStructureProvider>(new TOutliner()));
        }

        public static void Add<TType, TOutliner1, TOutliner2>(
            this ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>>.Builder builder)
            where TType : SyntaxNode
            where TOutliner1 : AbstractSyntaxStructureProvider, new()
            where TOutliner2 : AbstractSyntaxStructureProvider, new()
        {
            builder.Add(typeof(TType), ImmutableArray.Create<AbstractSyntaxStructureProvider>(new TOutliner1(), new TOutliner2()));
        }
    }
}
