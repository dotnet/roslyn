// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Context passed to an incremental generator when <see cref="IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext)"/> is called
    /// </summary>
    public readonly partial struct IncrementalGeneratorInitializationContext
    {
        private readonly ArrayBuilder<SyntaxInputNode> _syntaxInputBuilder;
        private readonly ArrayBuilder<IIncrementalGeneratorOutputNode> _outputNodes;
        private readonly string _sourceExtension;

        internal readonly ISyntaxHelper SyntaxHelper;
        internal readonly bool CatchAnalyzerExceptions;

        internal IncrementalGeneratorInitializationContext(
            ArrayBuilder<SyntaxInputNode> syntaxInputBuilder,
            ArrayBuilder<IIncrementalGeneratorOutputNode> outputNodes,
            ISyntaxHelper syntaxHelper,
            string sourceExtension,
            bool catchAnalyzerExceptions)
        {
            _syntaxInputBuilder = syntaxInputBuilder;
            _outputNodes = outputNodes;
            SyntaxHelper = syntaxHelper;
            _sourceExtension = sourceExtension;
            CatchAnalyzerExceptions = catchAnalyzerExceptions;
        }

        public SyntaxValueProvider SyntaxProvider => new(this, _syntaxInputBuilder, RegisterOutput, SyntaxHelper);

        public IncrementalValueProvider<Compilation> CompilationProvider => new IncrementalValueProvider<Compilation>(SharedInputNodes.Compilation.WithRegisterOutput(RegisterOutput).WithTrackingName(WellKnownGeneratorInputs.Compilation), CatchAnalyzerExceptions);

        // Use a ReferenceEqualityComparer as we want to rerun this stage whenever the CompilationOptions changes at all
        // (e.g. we don't care if it has the same conceptual value, we're ok rerunning as long as the actual instance
        // changes).
        internal IncrementalValueProvider<CompilationOptions> CompilationOptionsProvider
            => new(SharedInputNodes.CompilationOptions.WithRegisterOutput(RegisterOutput)
                .WithComparer(ReferenceEqualityComparer.Instance)
                .WithTrackingName(WellKnownGeneratorInputs.CompilationOptions), CatchAnalyzerExceptions);

        public IncrementalValueProvider<ParseOptions> ParseOptionsProvider => new IncrementalValueProvider<ParseOptions>(SharedInputNodes.ParseOptions.WithRegisterOutput(RegisterOutput).WithTrackingName(WellKnownGeneratorInputs.ParseOptions), CatchAnalyzerExceptions);

        public IncrementalValuesProvider<AdditionalText> AdditionalTextsProvider => new IncrementalValuesProvider<AdditionalText>(SharedInputNodes.AdditionalTexts.WithRegisterOutput(RegisterOutput).WithTrackingName(WellKnownGeneratorInputs.AdditionalTexts), CatchAnalyzerExceptions);

        public IncrementalValueProvider<AnalyzerConfigOptionsProvider> AnalyzerConfigOptionsProvider => new IncrementalValueProvider<AnalyzerConfigOptionsProvider>(SharedInputNodes.AnalyzerConfigOptions.WithRegisterOutput(RegisterOutput).WithTrackingName(WellKnownGeneratorInputs.AnalyzerConfigOptions), CatchAnalyzerExceptions);

        public IncrementalValuesProvider<MetadataReference> MetadataReferencesProvider => new IncrementalValuesProvider<MetadataReference>(SharedInputNodes.MetadataReferences.WithRegisterOutput(RegisterOutput).WithTrackingName(WellKnownGeneratorInputs.MetadataReferences), CatchAnalyzerExceptions);

        public void RegisterSourceOutput<TSource>(IncrementalValueProvider<TSource> source, Action<SourceProductionContext, TSource> action) => RegisterSourceOutput(source.Node, action, IncrementalGeneratorOutputKind.Source, _sourceExtension);

        public void RegisterSourceOutput<TSource>(IncrementalValuesProvider<TSource> source, Action<SourceProductionContext, TSource> action) => RegisterSourceOutput(source.Node, action, IncrementalGeneratorOutputKind.Source, _sourceExtension);

        public void RegisterImplementationSourceOutput<TSource>(IncrementalValueProvider<TSource> source, Action<SourceProductionContext, TSource> action) => RegisterSourceOutput(source.Node, action, IncrementalGeneratorOutputKind.Implementation, _sourceExtension);

        public void RegisterImplementationSourceOutput<TSource>(IncrementalValuesProvider<TSource> source, Action<SourceProductionContext, TSource> action) => RegisterSourceOutput(source.Node, action, IncrementalGeneratorOutputKind.Implementation, _sourceExtension);

        public void RegisterPostInitializationOutput(Action<IncrementalGeneratorPostInitializationContext> callback) => _outputNodes.Add(new PostInitOutputNode(callback.WrapUserAction(CatchAnalyzerExceptions)));

        private void RegisterOutput(IIncrementalGeneratorOutputNode outputNode)
        {
            if (!_outputNodes.Contains(outputNode))
            {
                _outputNodes.Add(outputNode);
            }
        }

        private void RegisterSourceOutput<TSource>(IIncrementalGeneratorNode<TSource> node, Action<SourceProductionContext, TSource> action, IncrementalGeneratorOutputKind kind, string sourceExt)
        {
            node.RegisterOutput(new SourceOutputNode<TSource>(node, action.WrapUserAction(CatchAnalyzerExceptions), kind, sourceExt));
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
        /// <remarks>
        /// Directory separators "/" and "\" are allowed in <paramref name="hintName"/>, they are normalized to "/" regardless of host platform.
        /// </remarks>
        public void AddSource(string hintName, SourceText sourceText) => AdditionalSources.Add(hintName, sourceText);
    }

    /// <summary>
    /// Context passed to an incremental generator when it has registered an output via <see cref="IncrementalGeneratorInitializationContext.RegisterSourceOutput{TSource}(IncrementalValueProvider{TSource}, Action{SourceProductionContext, TSource})"/>
    /// </summary>
    public readonly struct SourceProductionContext
    {
        internal readonly AdditionalSourcesCollection Sources;
        internal readonly DiagnosticBag Diagnostics;
        internal readonly Compilation Compilation;

        internal SourceProductionContext(AdditionalSourcesCollection sources, DiagnosticBag diagnostics, Compilation compilation, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            Sources = sources;
            Diagnostics = diagnostics;
            Compilation = compilation;
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
        /// <remarks>
        /// Directory separators "/" and "\" are allowed in <paramref name="hintName"/>, they are normalized to "/" regardless of host platform.
        /// </remarks>
        public void AddSource(string hintName, SourceText sourceText) => Sources.Add(hintName, sourceText);

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the users compilation
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="diagnostic"/> is located in a syntax tree which is not part of the compilation,
        /// its location span is outside of the given file, or its identifier is not valid.
        /// </exception>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, Compilation, isSupportedDiagnostic: static (_, _) => true, CancellationToken);
            Diagnostics.Add(diagnostic);
        }
    }

    // https://github.com/dotnet/roslyn/issues/53608 right now we only support generating source + diagnostics, but actively want to support generation of other things
    internal readonly struct IncrementalExecutionContext
    {
        internal readonly DiagnosticBag Diagnostics;

        internal readonly AdditionalSourcesCollection Sources;

        internal readonly DriverStateTable.Builder? TableBuilder;

        internal readonly GeneratorRunStateTable.Builder GeneratorRunStateBuilder;

        internal readonly ArrayBuilder<(string Key, string Value)> HostOutputBuilder;

        public IncrementalExecutionContext(DriverStateTable.Builder? tableBuilder, GeneratorRunStateTable.Builder generatorRunStateBuilder, AdditionalSourcesCollection sources)
        {
            TableBuilder = tableBuilder;
            GeneratorRunStateBuilder = generatorRunStateBuilder;
            Sources = sources;
            HostOutputBuilder = ArrayBuilder<(string, string)>.GetInstance();
            Diagnostics = DiagnosticBag.GetInstance();
        }

        internal (ImmutableArray<GeneratedSourceText> sources, ImmutableArray<Diagnostic> diagnostics, GeneratorRunStateTable executedSteps, ImmutableArray<(string Key, string Value)> hostOutputs) ToImmutableAndFree()
                => (Sources.ToImmutableAndFree(), Diagnostics.ToReadOnlyAndFree(), GeneratorRunStateBuilder.ToImmutableAndFree(), HostOutputBuilder.ToImmutableAndFree());

        internal void Free()
        {
            Sources.Free();
            Diagnostics.Free();
        }
    }
}
