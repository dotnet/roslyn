// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Wraps an incremental generator in a dummy <see cref="ISourceGenerator"/> interface.
    /// </summary>
    /// <remarks>
    /// Allows us to treat both generator types as ISourceGenerator externally and not change the public API.
    /// Inside the driver we unwrap and use the actual generator instance.
    /// </remarks>
    internal sealed class IncrementalGeneratorWrapper : ISourceGenerator
    {
        internal IIncrementalGenerator Generator { get; }

        public IncrementalGeneratorWrapper(IIncrementalGenerator generator)
        {
            this.Generator = generator;
        }

        // never used. Just for back compat with loading mechanism
        void ISourceGenerator.Execute(GeneratorExecutionContext context) => throw ExceptionUtilities.Unreachable();

        void ISourceGenerator.Initialize(GeneratorInitializationContext context) => throw ExceptionUtilities.Unreachable();
    }
}
