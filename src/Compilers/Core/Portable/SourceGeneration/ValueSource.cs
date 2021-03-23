// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{

    public struct GeneratorSource<T, U>
    {
        internal readonly INode<U> node;

        internal GeneratorSource(INode<U> node)
        {
            this.node = node;
        }

    }

    public struct GeneratorOutput
    {
        internal readonly IOutputNode node;

        internal GeneratorOutput(IOutputNode node)
        {
            this.node = node;
        }
    }

    struct ProductionContext { }


    static class ValueSourceExtensions
    {
        // 1 => many (or none) transform
        internal static GeneratorSource<IEnumerable<U>, U> Transform<T, U>(this GeneratorSource<IEnumerable<T>, T> source, Func<T, IEnumerable<U>> func) => new GeneratorSource<IEnumerable<U>, U>(new TransformNode<T, U>(source.node, func));

        // 1 => 1 transform (specialization of above)
        internal static GeneratorSource<IEnumerable<U>, U> Transform<T, U>(this GeneratorSource<IEnumerable<T>, T> source, Func<T, U> func) => new GeneratorSource<IEnumerable<U>, U>(new TransformNode<T, U>(source.node, func));

        // single => many (or none)
        internal static GeneratorSource<IEnumerable<U>, U> Transform<T, U>(this GeneratorSource<T, T> source, Func<T, IEnumerable<U>> func) => new GeneratorSource<IEnumerable<U>, U>(new TransformNode<T, U>(source.node, func));

        // single => single
        internal static GeneratorSource<U, U> Transform<T, U>(this GeneratorSource<T, T> source, Func<T, U> func) => new GeneratorSource<U, U>(new TransformNode<T, U>(source.node, func));



        // joing 1 => many ((source1[0], source2), (source1[1], source2) ...)
        internal static GeneratorSource<IEnumerable<(T, U)>, (T, U)> Combine<T, U>(this GeneratorSource<IEnumerable<T>, T> source1, GeneratorSource<U, U> source2) => new GeneratorSource<IEnumerable<(T, U)>, (T, U)>(new CombineNode<T, U>(source1.node, source2.node));

        // join 1 => 1 (source1, source2)
        internal static GeneratorSource<(T, U), (T, U)> Combine<T, U>(this GeneratorSource<T, T> source1, GeneratorSource<U, U> source2) => new GeneratorSource<(T, U), (T, U)>(new CombineNode<T, U>(source1.node, source2.node));


        // allow the user to override the comparison decision of if an element has changed for the underlying value source
        internal static GeneratorSource<IEnumerable<T>, T> WithComparer<T>(this GeneratorSource<IEnumerable<T>, T> source, IEqualityComparer<T> comparer) => new GeneratorSource<IEnumerable<T>, T>(source.node.WithComparer(comparer));

        internal static GeneratorSource<T, T> WithComparer<T>(this GeneratorSource<T, T> source, IEqualityComparer<T> comparer) => new GeneratorSource<T, T>(source.node.WithComparer(comparer));


        // convert between IMulti and ISingle values sources
        internal static GeneratorSource<IEnumerable<T>, IEnumerable<T>> Collect<T>(this GeneratorSource<IEnumerable<T>, T> source) => new GeneratorSource<IEnumerable<T>, IEnumerable<T>>(new CollectionNode<T>(source.node));

        internal static GeneratorSource<IEnumerable<T>, T> Unroll<T>(this GeneratorSource<IEnumerable<T>, IEnumerable<T>> source) => new GeneratorSource<IEnumerable<T>, T>(new UnrollNode<T>(source.node));


        // helper for filtering
        internal static GeneratorSource<IEnumerable<T>, T> Filter<T>(this GeneratorSource<IEnumerable<T>, T> source, Func<T, bool> filter)
        {
            return source.Transform((item) => (IEnumerable<T>)(filter(item) ? new[] { item } : Array.Empty<T>()));
        }


        // PROTOTYPE: naming. Does GenerateSource make it sound like you can't have multiple IGeneratorNodes?

        // 1 => 1 production
        internal static GeneratorOutput GenerateSource<T>(this GeneratorSource<IEnumerable<T>, T> source, Action<ProductionContext, T> action) => new GeneratorOutput(new OutputNode<T, ProductionContext>(source.node, action));

        // single => 1 production
        internal static GeneratorOutput GenerateSource<T>(this GeneratorSource<T, T> source, Action<ProductionContext, T> action) => new GeneratorOutput(new OutputNode<T, ProductionContext>(source.node, action));


    }

}
