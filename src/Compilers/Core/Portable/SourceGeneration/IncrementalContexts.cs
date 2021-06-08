// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Context passed to an incremental generator when <see cref="IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext)"/> is called
    /// </summary>
    public readonly struct IncrementalGeneratorInitializationContext
    {
        internal IncrementalGeneratorInitializationContext(CancellationToken cancellationToken)
        {
            this.CancellationToken = cancellationToken;
            InfoBuilder = new GeneratorInfo.Builder();
        }

        internal GeneratorInfo.Builder InfoBuilder { get; }

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
            InfoBuilder.PostInitCallback = callback;
        }

        public void RegisterExecutionPipeline(Action<IncrementalGeneratorPipelineContext> callback)
        {
            InfoBuilder.PipelineCallback = callback;
        }
    }

    public readonly struct IncrementalGeneratorPipelineContext
    {
        public IncrementalValueSources Sources { get; }

        internal IncrementalGeneratorPipelineContext(IncrementalValueSources valueSources)
        {
            Sources = valueSources;
        }
    }

    /// <summary>
    /// Context passed to the callback provided as part of producing sources
    /// </summary>
    public readonly struct SourceProductionContext
    {
        internal readonly ArrayBuilder<GeneratedSourceText> Sources;
        internal readonly DiagnosticBag Diagnostics;

        internal SourceProductionContext(ArrayBuilder<GeneratedSourceText> sources, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            Sources = sources;
            Diagnostics = diagnostics;
        }

        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds source code in the form of a <see cref="string"/> to the compilation.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="source">The source code to add to the compilation</param>
        public void AddSource(string hintName, string source) => AddSource(hintName, SourceText.From(source, Encoding.UTF8));

        /// <summary>
        /// Adds a <see cref="SourceText"/> to the compilation
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to add to the compilation</param>
        public void AddSource(string hintName, SourceText sourceText) => Sources.Add(new GeneratedSourceText(hintName, sourceText));

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the users compilation 
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);
    }

    // https://github.com/dotnet/roslyn/issues/53608 right now we only support generating source + diagnostics, but actively want to support generation of other things
    internal readonly struct IncrementalExecutionContext
    {
        internal readonly DiagnosticBag Diagnostics;

        internal readonly AdditionalSourcesCollection Sources;

        internal readonly DriverStateTable.Builder TableBuilder;

        public IncrementalExecutionContext(DriverStateTable.Builder tableBuilder, AdditionalSourcesCollection sources)
        {
            TableBuilder = tableBuilder;
            Sources = sources;
            Diagnostics = DiagnosticBag.GetInstance();
        }

        internal (ImmutableArray<GeneratedSourceText> sources, ImmutableArray<Diagnostic> diagnostics) ToImmutableAndFree()
                => (Sources.ToImmutableAndFree(), Diagnostics.ToReadOnlyAndFree());

        internal void Free()
        {
            Sources.Free();
            Diagnostics.Free();
        }
    }
}
