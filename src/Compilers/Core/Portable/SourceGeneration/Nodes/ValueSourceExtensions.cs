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
        public static IncrementalValueSource<U> Transform<T, U>(this IncrementalValueSource<T> source, Func<T, U> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // 1 => many (or none) transform
        internal static IncrementalValueSource<U> TransformMany<T, U>(this IncrementalValueSource<T> source, Func<T, ImmutableArray<U>> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // collection => collection
        internal static IncrementalValueSource<U> BatchTransform<T, U>(this IncrementalValueSource<T> source, Func<ImmutableArray<T>, U> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // single
        internal static IncrementalValueSource<U> BatchTransformMany<T, U>(this IncrementalValueSource<T> source, Func<ImmutableArray<T>, IEnumerable<U>> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.Node, func.WrapUserFunction()));

        // join many => many ((source1[0], source2), (source1[0], source2) ...)
        public static IncrementalValueSource<(T, IEnumerable<U>)> Join<T, U>(this IncrementalValueSource<T> source1, IncrementalValueSource<U> source2) => new IncrementalValueSource<(T, IEnumerable<U>)>(new JoinNode<T, U>(source1.Node, source2.Node));

        // helper for filtering
        public static IncrementalValueSource<T> Filter<T>(this IncrementalValueSource<T> source, Func<T, bool> filter)
        {
            return source.TransformMany((item) => filter(item) ? ImmutableArray.Create(item) : ImmutableArray<T>.Empty);
        }

        // 1 => 1 production
        public static IncrementalGeneratorOutput GenerateSource<T>(this IncrementalValueSource<T> source, Action<SourceProductionContext, T> action) => new IncrementalGeneratorOutput(new SourceOutputNode<T>(source.Node, action.WrapUserAction()));

        // custom comparer for given node
        public static IncrementalValueSource<T> WithComparer<T>(this IncrementalValueSource<T> source, IEqualityComparer<T> comparer) => new IncrementalValueSource<T>(source.Node.WithComparer(comparer));
    }
}
