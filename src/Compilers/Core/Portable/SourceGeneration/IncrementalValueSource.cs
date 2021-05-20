// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a value that can have transforms applied to it to construct an execution pipeline
    /// </summary>
    /// <remarks>
    /// This is an opaque type that cannot be used directly. Instead an <see cref="IIncrementalGenerator" />
    /// will receive a set of value sources when constructing its execution pipeline. A set of extension methods
    /// are then used to create transforms over the data that creates the actual pipeline.
    /// </remarks>
    /// <typeparam name="T">The type of value that this source provides access to</typeparam>
    public readonly struct IncrementalValueSource<T>
    {
        internal readonly IIncrementalGeneratorNode<T> Node;
        internal readonly Action<IIncrementalGeneratorOutputNode> RegisterOutput;

        internal IncrementalValueSource(IIncrementalGeneratorNode<T> node, Action<IIncrementalGeneratorOutputNode> registerOutput)
        {
            this.Node = node;
            this.RegisterOutput = registerOutput;
        }
    }
}
