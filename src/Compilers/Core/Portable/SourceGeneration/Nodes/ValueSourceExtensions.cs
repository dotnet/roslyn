// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public static class IncrementalValueSourceExtensions
    {
        // 1 => 1 transform 
        public static IncrementalValueSource<U> Transform<T, U>(this IncrementalValueSource<T> source, Func<T, U> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.Node, func.WrapUserFunction()), source.RegisterOutput);

        // 1 => many (or none) transform
        public static IncrementalValueSource<U> TransformMany<T, U>(this IncrementalValueSource<T> source, Func<T, ImmutableArray<U>> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.Node, func.WrapUserFunction()), source.RegisterOutput);

        // 1 => many (or none) transform with enumerable
        public static IncrementalValueSource<U> TransformMany<T, U>(this IncrementalValueSource<T> source, Func<T, IEnumerable<U>> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.Node, t => func.WrapUserFunction()(t).AsImmutable()), source.RegisterOutput);

        // collection => collection
        public static IncrementalValueSource<U> BatchTransform<T, U>(this IncrementalValueSource<T> source, Func<ImmutableArray<T>, U> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.Node, func.WrapUserFunction()), source.RegisterOutput);

        // single
        public static IncrementalValueSource<U> BatchTransformMany<T, U>(this IncrementalValueSource<T> source, Func<ImmutableArray<T>, ImmutableArray<U>> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.Node, func.WrapUserFunction()), source.RegisterOutput);

        // single (enumerable)
        public static IncrementalValueSource<U> BatchTransformMany<T, U>(this IncrementalValueSource<T> source, Func<ImmutableArray<T>, IEnumerable<U>> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.Node, t => func.WrapUserFunction()(t).ToImmutableArray()), source.RegisterOutput);


        // join many => many ((source1[0], source2), (source1[0], source2) ...)
        public static IncrementalValueSource<(T, ImmutableArray<U>)> Join<T, U>(this IncrementalValueSource<T> source1, IncrementalValueSource<U> source2) => new IncrementalValueSource<(T, ImmutableArray<U>)>(new JoinNode<T, U>(source1.Node, source2.Node), source1.RegisterOutput);

        // helper for filtering
        public static IncrementalValueSource<T> Filter<T>(this IncrementalValueSource<T> source, Func<T, bool> filter)
        {
            return source.TransformMany((item) => filter(item) ? ImmutableArray.Create(item) : ImmutableArray<T>.Empty);
        }

        // 1 => 1 production
        public static void GenerateSource<T>(this IncrementalValueSource<T> source, Action<SourceProductionContext, T> action) => source.RegisterOutput(new SourceOutputNode<T>(source.Node, action.WrapUserAction()));

        // custom comparer for given node
        public static IncrementalValueSource<T> WithComparer<T>(this IncrementalValueSource<T> source, IEqualityComparer<T> comparer) => new IncrementalValueSource<T>(source.Node.WithComparer(comparer), source.RegisterOutput);
    }
}
