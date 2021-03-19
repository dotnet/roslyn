// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public struct MultiValueSource<T>
    {
        internal readonly INode<T> node;

        internal MultiValueSource(INode<T> node)
        {
            this.node = node;
        }

    }

    public struct SingleValueSource<T>
    {
        internal readonly INode<T> node;

        internal SingleValueSource(INode<T> node)
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
        internal static MultiValueSource<U> Transform<T, U>(this MultiValueSource<T> source, Func<T, IEnumerable<U>> func) => new MultiValueSource<U>(new TransformNode<T, U>(source.node, func));

        // 1 => 1 transform (specialization of above)
        internal static MultiValueSource<U> Transform<T, U>(this MultiValueSource<T> source, Func<T, U> func) => new MultiValueSource<U>(new TransformNode<T, U>(source.node, func));

        // single => many (or none)
        internal static MultiValueSource<U> Transform<T, U>(this SingleValueSource<T> source, Func<T, IEnumerable<U>> func) => new MultiValueSource<U>(new TransformNode<T, U>(source.node, func));

        // single => single
        internal static SingleValueSource<U> Transform<T, U>(this SingleValueSource<T> source, Func<T, U> func) => new SingleValueSource<U>(new TransformNode<T, U>(source.node, func));



        // joing 1 => many ((source1[0], source2), (source1[1], source2) ...)
        internal static MultiValueSource<(T, U)> Combine<T, U>(this MultiValueSource<T> source1, SingleValueSource<U> source2) => new MultiValueSource<(T, U)>(new CombineNode<T, U>(source1.node, source2.node));

        // join 1 => 1 (source1, source2)
        internal static SingleValueSource<(T, U)> Combine<T, U>(this SingleValueSource<T> source1, SingleValueSource<U> source2) => new SingleValueSource<(T, U)>(new CombineNode<T, U>(source1.node, source2.node));


        // allow the user to override the comparison decision of if an element has changed for the underlying value source
        internal static MultiValueSource<T> WithComparer<T>(this MultiValueSource<T> source, IEqualityComparer<T> comparer) => new MultiValueSource<T>(source.node.WithComparer(comparer));

        internal static SingleValueSource<T> WithComparer<T>(this SingleValueSource<T> source, IEqualityComparer<T> comparer) => new SingleValueSource<T>(source.node.WithComparer(comparer));


        // convert between IMulti and ISingle values sources
        internal static SingleValueSource<IEnumerable<T>> Collect<T>(this MultiValueSource<T> source) => new SingleValueSource<IEnumerable<T>>(new CollectionNode<T>(source.node));

        internal static MultiValueSource<T> Unroll<T>(this SingleValueSource<IEnumerable<T>> source) => new MultiValueSource<T>(new UnrollNode<T>(source.node));


        // helper for filtering
        internal static MultiValueSource<T> Filter<T>(this MultiValueSource<T> source, Func<T, bool> filter)
        {
            return source.Transform((item) => (IEnumerable<T>)(filter(item) ? new[] { item } : Array.Empty<T>()));
        }


        // PROTOTYPE: naming. Does GenerateSource make it sound like you can't have multiple IGeneratorNodes?

        // 1 => 1 production
        internal static GeneratorOutput GenerateSource<T>(this MultiValueSource<T> source, Action<ProductionContext, T> action) => new GeneratorOutput(new OutputNode<T, ProductionContext>(source.node, action));

        // single => 1 production
        internal static GeneratorOutput GenerateSource<T>(this SingleValueSource<T> source, Action<ProductionContext, T> action) => new GeneratorOutput(new OutputNode<T, ProductionContext>(source.node, action));


    }

}
