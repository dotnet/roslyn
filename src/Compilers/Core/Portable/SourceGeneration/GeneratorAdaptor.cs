// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Wraps an incremental generator in a dummy <see cref="ISourceGenerator"/> interface.
    /// </summary>
    /// <remarks>
    /// Used to unify loading incremental generators with older ISourceGenerator style ones
    /// </remarks>
    /// <typeparam name="TIncrementalGenerator">The type of the incrmental generator being wrapped</typeparam>
    internal class IncrementalToSourceGeneratorWrapper<TIncrementalGenerator> : IncrementalGeneratorWrapper, ISourceGenerator
        where TIncrementalGenerator : IIncrementalGenerator
    {
        public IncrementalToSourceGeneratorWrapper(TIncrementalGenerator generator)
            : base(generator)
        {
        }

        // never used. Just for back compat with loading mechansim
        void ISourceGenerator.Execute(GeneratorExecutionContext context) => throw new NotImplementedException();

        void ISourceGenerator.Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
    }

    /// <summary>
    /// Non generic wrapper for incremental generators
    /// </summary>
    /// <remarks>
    /// The generic version allows us to ensure we get a unique type per wrapper externally
    /// Internally we can just use this to grab the actual generator instance we care about.
    /// </remarks>
    internal class IncrementalGeneratorWrapper
    {
        internal IIncrementalGenerator Generator { get; }

        public IncrementalGeneratorWrapper(IIncrementalGenerator generator)
        {
            this.Generator = generator;
        }
    }
}
