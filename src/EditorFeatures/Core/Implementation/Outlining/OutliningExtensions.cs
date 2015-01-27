// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal static class OutliningExtensions
    {
        public static void Add<TType, TOutliner>(
            this ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxNodeOutliner>>.Builder builder)
            where TType : SyntaxNode
            where TOutliner : AbstractSyntaxNodeOutliner, new()
        {
            builder.Add(typeof(TType), ImmutableArray.Create<AbstractSyntaxNodeOutliner>(new TOutliner()));
        }

        public static void Add<TType, TOutliner1, TOutliner2>(
            this ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxNodeOutliner>>.Builder builder)
            where TType : SyntaxNode
            where TOutliner1 : AbstractSyntaxNodeOutliner, new()
            where TOutliner2 : AbstractSyntaxNodeOutliner, new()
        {
            builder.Add(typeof(TType), ImmutableArray.Create<AbstractSyntaxNodeOutliner>(new TOutliner1(), new TOutliner2()));
        }
    }
}
