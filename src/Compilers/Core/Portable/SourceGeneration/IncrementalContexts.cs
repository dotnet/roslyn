﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Context passed to an incremental generator when <see cref="IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext)"/> is called
    /// </summary>
    public readonly struct IncrementalGeneratorInitializationContext
    {
        private readonly ArrayBuilder<ISyntaxInputNode> _syntaxInputBuilder;
        private readonly ArrayBuilder<IIncrementalGeneratorOutputNode> _outputNodes;

        internal IncrementalGeneratorInitializationContext(ArrayBuilder<ISyntaxInputNode> syntaxInputBuilder, ArrayBuilder<IIncrementalGeneratorOutputNode> outputNodes)
        {
            _syntaxInputBuilder = syntaxInputBuilder;
            _outputNodes = outputNodes;
        }

        public SyntaxValueProvider SyntaxProvider => new SyntaxValueProvider(_syntaxInputBuilder, RegisterOutput);

        public IncrementalValueProvider<Compilation> CompilationProvider => new IncrementalValueProvider<Compilation>(SharedInputNodes.Compilation.WithRegisterOutput(RegisterOutput));

        public IncrementalValueProvider<ParseOptions> ParseOptionsProvider => new IncrementalValueProvider<ParseOptions>(SharedInputNodes.ParseOptions.WithRegisterOutput(RegisterOutput));

        public IncrementalValuesProvider<AdditionalText> AdditionalTextsProvider => new IncrementalValuesProvider<AdditionalText>(SharedInputNodes.AdditionalTexts.WithRegisterOutput(RegisterOutput));

        public IncrementalValueProvider<AnalyzerConfigOptionsProvider> AnalyzerConfigOptionsProvider => new IncrementalValueProvider<AnalyzerConfigOptionsProvider>(SharedInputNodes.AnalyzerConfigOptions.WithRegisterOutput(RegisterOutput));

        public void RegisterSourceOutput<TSource>(IncrementalValueProvider<TSource> source, Action<SourceProductionContext, TSource> action) => source.Node.RegisterOutput(new SourceOutputNode<TSource>(source.Node, action.WrapUserAction()));

        public void RegisterSourceOutput<TSource>(IncrementalValuesProvider<TSource> source, Action<SourceProductionContext, TSource> action) => source.Node.RegisterOutput(new SourceOutputNode<TSource>(source.Node, action.WrapUserAction()));

        public void RegisterPostInitializationOutput(Action<IncrementalGeneratorPostInitializationContext> callback) => _outputNodes.Add(new PostInitOutputNode(callback.WrapUserAction()));

        private void RegisterOutput(IIncrementalGeneratorOutputNode outputNode)
        {
            if (!_outputNodes.Contains(outputNode))
            {
                _outputNodes.Add(outputNode);
            }
        }
    }

    /// <summary>
    /// Context passed to an incremental generator when it has registered an output via <see cref="IncrementalGeneratorInitializationContext.RegisterPostInitializationOutput(Action{IncrementalGeneratorPostInitializationContext})"/>
    /// </summary>
    public readonly struct IncrementalGeneratorPostInitializationContext
    {
        internal readonly AdditionalSourcesCollection AdditionalSources;

        internal IncrementalGeneratorPostInitializationContext(AdditionalSourcesCollection additionalSources, CancellationToken cancellationToken)
        {
            AdditionalSources = additionalSources;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// A <see cref="System.Threading.CancellationToken"/> that can be checked to see if the PostInitialization should be cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds source code in the form of a <see cref="string"/> to the compilation that will be available during subsequent phases
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="source">The source code to add to the compilation</param>
        public void AddSource(string hintName, string source) => AddSource(hintName, SourceText.From(source, Encoding.UTF8));

        /// <summary>
        /// Adds a <see cref="SourceText"/> to the compilation that will be available during subsequent phases
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to add to the compilation</param>
        public void AddSource(string hintName, SourceText sourceText) => AdditionalSources.Add(hintName, sourceText);
    }

    /// <summary>
    /// Context passed to an incremental generator when it has registered an output via <see cref="IncrementalGeneratorInitializationContext.RegisterSourceOutput{TSource}(IncrementalValueProvider{TSource}, Action{SourceProductionContext, TSource})"/>
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

        internal readonly DriverStateTable.Builder? TableBuilder;

        public IncrementalExecutionContext(DriverStateTable.Builder? tableBuilder, AdditionalSourcesCollection sources)
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
