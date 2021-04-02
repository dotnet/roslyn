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
    internal class IncrementalToSourceGeneratorWrapper<TIncrementalGenerator> : ISourceGenerator
        where TIncrementalGenerator : IIncrementalGenerator
    {
        internal TIncrementalGenerator IncrementalGenerator { get; }

        public IncrementalToSourceGeneratorWrapper(TIncrementalGenerator generator)
        {
            this.IncrementalGenerator = generator;
        }

        // never used. Just for back compat with loading mechansim
        void ISourceGenerator.Execute(GeneratorExecutionContext context) => throw new NotImplementedException();

        void ISourceGenerator.Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
    }
}
