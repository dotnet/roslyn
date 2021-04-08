// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    /// <summary>
    /// Adapts an ISourceGenerator to an incremental generator that
    /// by providng an execution environment that matches the old one
    /// </summary>
    internal class SourceGeneratorAdaptor : IIncrementalGenerator
    {
        internal ISourceGenerator SourceGenerator { get; }

        public SourceGeneratorAdaptor(ISourceGenerator generator)
        {
            SourceGenerator = generator;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // PROTOTYPE: we'll call initialize on the underlying generator and pull out any info that is needed
            //            then we'll set PostInit etc on our context and register a pipleine that
            //            executes the old generator in the new framework

            GeneratorInitializationContext oldContext = new GeneratorInitializationContext(context.CancellationToken);
            try
            {
                SourceGenerator.Initialize(oldContext);
            }
            catch (Exception)
            {
                // PROTOTYPE 
                // wrap in a user func exception?
                throw;
            }

            if (oldContext.InfoBuilder.PostInitCallback is object)
            {
                context.RegisterForPostInitialization(oldContext.InfoBuilder.PostInitCallback);
            }

            context.RegisterExecutionPipeline((ctx) =>
            {

            });
        }
    }
}
