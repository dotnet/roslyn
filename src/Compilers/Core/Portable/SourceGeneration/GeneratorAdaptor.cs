// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Adapts an ISourceGenerator to an incremental generator that
    /// by providing an execution environment that matches the old one
    /// </summary>
    internal sealed class SourceGeneratorAdaptor : IIncrementalGenerator
    {
        internal ISourceGenerator SourceGenerator { get; }

        public SourceGeneratorAdaptor(ISourceGenerator generator)
        {
            SourceGenerator = generator;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // PROTOTYPE(source-generators):
            // we'll call initialize on the underlying generator and pull out any info that is needed
            // then we'll set PostInit etc on our context and register a pipleine that
            // executes the old generator in the new framework

            GeneratorInitializationContext oldContext = new GeneratorInitializationContext(context.CancellationToken);
            SourceGenerator.Initialize(oldContext);

            context.InfoBuilder.PostInitCallback = oldContext.InfoBuilder.PostInitCallback;
            context.InfoBuilder.SyntaxContextReceiverCreator = oldContext.InfoBuilder.SyntaxContextReceiverCreator;

            context.RegisterExecutionPipeline((ctx) =>
            {
                // PROTOTYPE(source-generators): this is where we'll build the actual emulation pipeline
            });
        }
    }
}
