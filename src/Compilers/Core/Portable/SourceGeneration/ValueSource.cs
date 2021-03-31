// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{

    public struct IncrementalValueSource<T>
    {
        internal readonly INode<T> node;

        internal IncrementalValueSource(INode<T> node)
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
        // 1 => 1 transform 
        internal static IncrementalValueSource<U> Transform<T, U>(this IncrementalValueSource<T> source, Func<T, U> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.node, func.WrapUserFunction()));

        // 1 => many (or none) transform
        internal static IncrementalValueSource<U> TransformMany<T, U>(this IncrementalValueSource<T> source, Func<T, IEnumerable<U>> func) => new IncrementalValueSource<U>(new TransformNode<T, U>(source.node, func.WrapUserFunction()));

        // collection => collection
        internal static IncrementalValueSource<U> BatchTransform<T, U>(this IncrementalValueSource<T> source, Func<IEnumerable<T>, U> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.node, func.WrapUserFunction()));

        // single
        internal static IncrementalValueSource<U> BatchTransformMany<T, U>(this IncrementalValueSource<T> source, Func<IEnumerable<T>, IEnumerable<U>> func) => new IncrementalValueSource<U>(new BatchTransformNode<T, U>(source.node, func.WrapUserFunction()));

        // join many => many ((source1[0], source2), (source1[1], source2) ...)
        internal static IncrementalValueSource<(T, U)> Join<T, U>(this IncrementalValueSource<T> source1, IncrementalValueSource<U> source2) => new IncrementalValueSource<(T, U)>(new JoinNode<T, U>(source1.node, source2.node));

        // helper for filtering
        internal static IncrementalValueSource<T> Filter<T>(this IncrementalValueSource<T> source, Func<T, bool> filter)
        {
            return source.TransformMany((item) => (IEnumerable<T>)(filter(item) ? new[] { item } : Array.Empty<T>()));
        }

        // PROTOTYPE: naming. Does GenerateSource make it sound like you can't have multiple IGeneratorNodes?

        // 1 => 1 production
        internal static GeneratorOutput GenerateSource<T>(this IncrementalValueSource<T> source, Action<ProductionContext, T> action) => new GeneratorOutput(new OutputNode<T, ProductionContext>(source.node, action));

        // single => 1 production
        internal static GeneratorOutput GenerateSourceBatch<T>(this IncrementalValueSource<T> source, Action<ProductionContext, IEnumerable<T>> action) => new GeneratorOutput(new OutputNode<T, ProductionContext>(source.node, action));

    }


}
