// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a provider of a single value that can be transformed as part of constructing an execution pipeline
    /// </summary>
    /// <remarks>
    /// This is an opaque type that cannot be used directly. Instead an <see cref="IIncrementalGenerator" />
    /// will receive a set of value providers when constructing its execution pipeline. A set of extension methods
    /// are then used to create transforms over the data that creates the actual pipeline.
    /// </remarks>
    /// <typeparam name="TValue">The type of value that this source provides access to</typeparam>
    public readonly struct IncrementalValueProvider<TValue>
    {
        internal readonly IIncrementalGeneratorNode<TValue> Node;
        internal readonly bool CatchAnalyzerExceptions;

        internal IncrementalValueProvider(IIncrementalGeneratorNode<TValue> node, bool catchAnalyzerExceptions)
        {
            this.Node = node;
            this.CatchAnalyzerExceptions = catchAnalyzerExceptions;
        }
    }

    /// <summary>
    /// Represents a provider of multiple values that can be transformed to construct an execution pipeline
    /// </summary>
    /// <remarks>
    /// This is an opaque type that cannot be used directly. Instead an <see cref="IIncrementalGenerator" />
    /// will receive a set of value providers when constructing its execution pipeline. A set of extension methods
    /// are then used to create transforms over the data that creates the actual pipeline.
    /// </remarks>
    /// <typeparam name="TValues">The type of value that this source provides access to</typeparam>
    public readonly struct IncrementalValuesProvider<TValues>
    {
        internal readonly IIncrementalGeneratorNode<TValues> Node;
        internal readonly bool CatchAnalyzerExceptions;

        internal IncrementalValuesProvider(IIncrementalGeneratorNode<TValues> node, bool catchAnalyzerExceptions)
        {
            this.Node = node;
            this.CatchAnalyzerExceptions = catchAnalyzerExceptions;
        }
    }
}
