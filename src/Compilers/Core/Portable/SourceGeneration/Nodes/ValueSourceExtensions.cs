// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(source-generators): these are just internal stubs for now, that let us build the API surface up
    internal static class IncrementalValueSourceExtensions
    {
        // 1 => 1 transform 
        internal static IncrementalValueSource<U> Transform<T, U>(this IncrementalValueSource<T> source, Func<T, U> func) => default;

        // 1 => many (or none) transform
        internal static IncrementalValueSource<U> TransformMany<T, U>(this IncrementalValueSource<T> source, Func<T, IEnumerable<U>> func) => default;

        // collection => collection
        internal static IncrementalValueSource<U> BatchTransform<T, U>(this IncrementalValueSource<T> source, Func<IEnumerable<T>, U> func) => default;

        // single
        internal static IncrementalValueSource<U> BatchTransformMany<T, U>(this IncrementalValueSource<T> source, Func<IEnumerable<T>, IEnumerable<U>> func) => default;

        // join many => many ((source1[0], source2), (source1[0], source2) ...)
        internal static IncrementalValueSource<(T, IEnumerable<U>)> Join<T, U>(this IncrementalValueSource<T> source1, IncrementalValueSource<U> source2) => new IncrementalValueSource<(T, IEnumerable<U>)>(new JoinNode<T, U>(source1.node, source2.node));

        // helper for filtering
        internal static IncrementalValueSource<T> Filter<T>(this IncrementalValueSource<T> source, Func<T, bool> filter)
        {
            return source.TransformMany((item) => (IEnumerable<T>)(filter(item) ? new[] { item } : Array.Empty<T>()));
        }

        // PROTOTYPE(source-generators): naming. Does GenerateSource make it sound like you can't have multiple IncrementalGeneratorOutputs?

        // 1 => 1 production
        internal static IncrementalGeneratorOutput GenerateSource<T>(this IncrementalValueSource<T> source, Action<SourceProductionContext, T> action) => new IncrementalGeneratorOutput(new SourceOutputNode<T>(source.node, action.WrapUserAction()));

        // single => 1 production
        internal static IncrementalGeneratorOutput GenerateSourceBatch<T>(this IncrementalValueSource<T> source, Action<SourceProductionContext, IEnumerable<T>> action) => default;
    }
}
