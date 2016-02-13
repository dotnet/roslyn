// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal static class OutliningExtensions
    {
        public static void Add<TType, TOutliner>(
            this ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>>.Builder builder)
            where TType : SyntaxNode
            where TOutliner : AbstractSyntaxOutliner, new()
        {
            builder.Add(typeof(TType), ImmutableArray.Create<AbstractSyntaxOutliner>(new TOutliner()));
        }

        public static void Add<TType, TOutliner1, TOutliner2>(
            this ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>>.Builder builder)
            where TType : SyntaxNode
            where TOutliner1 : AbstractSyntaxOutliner, new()
            where TOutliner2 : AbstractSyntaxOutliner, new()
        {
            builder.Add(typeof(TType), ImmutableArray.Create<AbstractSyntaxOutliner>(new TOutliner1(), new TOutliner2()));
        }
    }
}
