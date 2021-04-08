// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Context passed to an incremental generator when <see cref="IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext)"/> is called
    /// </summary>
    public readonly struct IncrementalGeneratorInitializationContext
    {
        /// <summary>
        /// A <see cref="System.Threading.CancellationToken"/> that can be checked to see if the initialization should be cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Register a callback that is invoked after initialization.
        /// </summary>
        /// <remarks>
        /// This method allows a generator to opt-in to an extra phase in the generator lifecycle called PostInitialization. After being initialized
        /// any incremental generators that have opted in will have their provided callback invoked with a <see cref="GeneratorPostInitializationContext"/> instance
        /// that can be used to alter the compilation that is provided to subsequent generator phases.
        /// 
        /// For example a generator may choose to add sources during PostInitialization. These will be added to the compilation before the incremental pipeline is 
        /// invoked and will appear alongside user provided <see cref="SyntaxTree"/>s, and made available for semantic analysis. 
        /// 
        /// Note that any sources added during PostInitialization <i>will</i> be visible to the later phases of other generators operating on the compilation. 
        /// </remarks>
        /// <param name="callback">An <see cref="Action{T}"/> that accepts a <see cref="GeneratorPostInitializationContext"/> that will be invoked after initialization.</param>
        public void RegisterForPostInitialization(Action<GeneratorPostInitializationContext> callback)
        {
            // PROTOTYPE(source-generators): should we share the post init context with the V1 api or make a duplicate context?
            // PROTOTYPE(source-generators): public api stub
        }

        public void RegisterExecutionPipeline(Action<IncrementalGeneratorPipelineContext> callback)
        {
            // PROTOTYPE(source-generators): should this be a required method on the interface?
            // PROTOTYPE(source-generators): public api stub
        }
    }

    public readonly struct IncrementalGeneratorPipelineContext
    {
        public void RegisterOutput(IncrementalGeneratorOutput output)
        {
            // PROTOTYPE(source-generators): public api stub
        }
    }

    /// <summary>
    /// Context passed to the callback provided as part of <see cref="IncrementalValueSourceExtensions.GenerateSource{T}(IncrementalValueSource{T}, Action{SourceProductionContext, T})"/>
    /// </summary>
    public readonly struct SourceProductionContext
    {
        public void AddSource(string name, string content) { }
    }
}
