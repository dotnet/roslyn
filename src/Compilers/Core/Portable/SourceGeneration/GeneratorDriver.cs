// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Responsible for orchestrating a source generation pass
    /// </summary>
    /// <remarks>
    /// GeneratorDriver is an immutable class that can be manipulated by returning a mutated copy of itself.
    /// In the compiler we only ever create a single instance and ignore the mutated copy. The IDE may perform 
    /// multiple edits, or generation passes of the same driver, re-using the state as needed.
    /// </remarks>
    public abstract class GeneratorDriver
    {
        internal const IncrementalGeneratorOutputKind HostKind = (IncrementalGeneratorOutputKind)0b100000; // several steps higher than IncrementalGeneratorOutputKind.Implementation

        internal readonly GeneratorDriverState _state;

        internal GeneratorDriver(GeneratorDriverState state)
        {
            Debug.Assert(state.Generators.GroupBy(s => s.GetGeneratorType()).Count() == state.Generators.Length); // ensure we don't have duplicate generator types
            _state = state;
        }

        internal GeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts, GeneratorDriverOptions driverOptions)
        {
            var incrementalGenerators = GetIncrementalGenerators(generators, SourceExtension);
            _state = new GeneratorDriverState(parseOptions, optionsProvider, generators, incrementalGenerators, additionalTexts, generators.SelectAsArray(g => GeneratorState.Empty), DriverStateTable.Empty, SyntaxStore.Empty, driverOptions, runtime: TimeSpan.Zero, parseOptionsChanged: true);
        }

        public GeneratorDriver RunGenerators(Compilation compilation, CancellationToken cancellationToken = default)
        {
            var state = RunGeneratorsCore(compilation, diagnosticsBag: null, cancellationToken); //don't directly collect diagnostics on this path
            return FromState(state);
        }

        public GeneratorDriver RunGeneratorsAndUpdateCompilation(Compilation compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken = default)
        {
            var diagnosticsBag = DiagnosticBag.GetInstance();
            var state = RunGeneratorsCore(compilation, diagnosticsBag, cancellationToken);

            // build the output compilation
            diagnostics = diagnosticsBag.ToReadOnlyAndFree();
            ArrayBuilder<SyntaxTree> trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var generatorState in state.GeneratorStates)
            {
                trees.AddRange(generatorState.PostInitTrees.Select(t => t.Tree));
                trees.AddRange(generatorState.GeneratedTrees.Select(t => t.Tree));
            }
            outputCompilation = compilation.AddSyntaxTrees(trees);
            trees.Free();

            return FromState(state);
        }

        public GeneratorDriver AddGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            var incrementalGenerators = GetIncrementalGenerators(generators, SourceExtension);
            var newState = _state.With(sourceGenerators: _state.Generators.AddRange(generators),
                                       incrementalGenerators: _state.IncrementalGenerators.AddRange(incrementalGenerators),
                                       generatorStates: _state.GeneratorStates.AddRange(new GeneratorState[generators.Length]));
            return FromState(newState);
        }

        public GeneratorDriver ReplaceGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            var incrementalGenerators = GetIncrementalGenerators(generators, SourceExtension);
            var states = ArrayBuilder<GeneratorState>.GetInstance(generators.Length);

            foreach (var generator in generators)
            {
                var existingIndex = _state.Generators.IndexOf(generator);

                if (existingIndex >= 0)
                {
                    states.Add(_state.GeneratorStates[existingIndex]);
                }
                else
                {
                    states.Add(GeneratorState.Empty);
                }
            }

            return FromState(_state.With(generators, incrementalGenerators, states.ToImmutableAndFree()));
        }

        public GeneratorDriver RemoveGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            var newGenerators = _state.Generators;
            var newStates = _state.GeneratorStates;
            var newIncrementalGenerators = _state.IncrementalGenerators;
            for (int i = 0; i < newGenerators.Length; i++)
            {
                if (generators.Contains(newGenerators[i]))
                {
                    newGenerators = newGenerators.RemoveAt(i);
                    newStates = newStates.RemoveAt(i);
                    newIncrementalGenerators = newIncrementalGenerators.RemoveAt(i);
                    i--;
                }
            }

            return FromState(_state.With(sourceGenerators: newGenerators, incrementalGenerators: newIncrementalGenerators, generatorStates: newStates));
        }

        public GeneratorDriver AddAdditionalTexts(ImmutableArray<AdditionalText> additionalTexts)
        {
            var newState = _state.With(additionalTexts: _state.AdditionalTexts.AddRange(additionalTexts));
            return FromState(newState);
        }

        public GeneratorDriver RemoveAdditionalTexts(ImmutableArray<AdditionalText> additionalTexts)
        {
            var newState = _state.With(additionalTexts: _state.AdditionalTexts.RemoveRange(additionalTexts));
            return FromState(newState);
        }

        public GeneratorDriver ReplaceAdditionalText(AdditionalText oldText, AdditionalText newText)
        {
            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }
            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            var newState = _state.With(additionalTexts: _state.AdditionalTexts.Replace(oldText, newText));
            return FromState(newState);
        }

        public GeneratorDriver ReplaceAdditionalTexts(ImmutableArray<AdditionalText> newTexts) => FromState(_state.With(additionalTexts: newTexts));

        public GeneratorDriver WithUpdatedParseOptions(ParseOptions newOptions) => newOptions is object
                                                                                   ? FromState(_state.With(parseOptions: newOptions, parseOptionsChanged: true))
                                                                                   : throw new ArgumentNullException(nameof(newOptions));

        public GeneratorDriver WithUpdatedAnalyzerConfigOptions(AnalyzerConfigOptionsProvider newOptions) => newOptions is object
                                                                                                             ? FromState(_state.With(optionsProvider: newOptions))
                                                                                                             : throw new ArgumentNullException(nameof(newOptions));

        public GeneratorDriverRunResult GetRunResult()
        {
            var results = _state.Generators.ZipAsArray(
                            _state.GeneratorStates,
                            (generator, generatorState)
                                => new GeneratorRunResult(generator,
                                                          diagnostics: generatorState.Diagnostics,
                                                          exception: generatorState.Exception,
                                                          generatedSources: getGeneratorSources(generatorState),
                                                          elapsedTime: generatorState.ElapsedTime,
                                                          namedSteps: generatorState.ExecutedSteps,
                                                          outputSteps: generatorState.OutputSteps,
                                                          hostOutputs: generatorState.HostOutputs));
            return new GeneratorDriverRunResult(results, _state.RunTime);

            static ImmutableArray<GeneratedSourceResult> getGeneratorSources(GeneratorState generatorState)
            {
                ArrayBuilder<GeneratedSourceResult> sources = ArrayBuilder<GeneratedSourceResult>.GetInstance(generatorState.PostInitTrees.Length + generatorState.GeneratedTrees.Length);
                foreach (var tree in generatorState.PostInitTrees)
                {
                    sources.Add(new GeneratedSourceResult(tree.Tree, tree.Text, tree.HintName));
                }
                foreach (var tree in generatorState.GeneratedTrees)
                {
                    sources.Add(new GeneratedSourceResult(tree.Tree, tree.Text, tree.HintName));
                }
                return sources.ToImmutableAndFree();
            }
        }

        public GeneratorDriverTimingInfo GetTimingInfo()
        {
            var generatorTimings = _state.Generators.ZipAsArray(_state.GeneratorStates, (generator, generatorState) => new GeneratorTimingInfo(generator, generatorState.ElapsedTime));
            return new GeneratorDriverTimingInfo(_state.RunTime, generatorTimings);
        }

        internal GeneratorDriverState RunGeneratorsCore(Compilation compilation, DiagnosticBag? diagnosticsBag, CancellationToken cancellationToken = default)
        {
            // with no generators, there is no work to do
            if (_state.Generators.IsEmpty)
            {
                return _state.With(stateTable: DriverStateTable.Empty, runTime: TimeSpan.Zero);
            }

            // run the actual generation
            using var timer = CodeAnalysisEventSource.Log.CreateGeneratorDriverRunTimer();
            var state = _state;
            var stateBuilder = ArrayBuilder<GeneratorState>.GetInstance(state.Generators.Length);
            var constantSourcesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            var syntaxInputNodes = ArrayBuilder<SyntaxInputNode>.GetInstance();

            for (int i = 0; i < state.IncrementalGenerators.Length; i++)
            {
                var generator = state.IncrementalGenerators[i];
                var generatorState = state.GeneratorStates[i];
                var sourceGenerator = state.Generators[i];

                // initialize the generator if needed
                if (!generatorState.Initialized)
                {
                    var outputBuilder = ArrayBuilder<IIncrementalGeneratorOutputNode>.GetInstance();
                    var inputBuilder = ArrayBuilder<SyntaxInputNode>.GetInstance();
                    var postInitSources = ImmutableArray<GeneratedSyntaxTree>.Empty;
                    var pipelineContext = new IncrementalGeneratorInitializationContext(
                        inputBuilder, outputBuilder, this.SyntaxHelper, this.SourceExtension, compilation.CatchAnalyzerExceptions);

                    Exception? ex = null;
                    try
                    {
                        generator.Initialize(pipelineContext);
                    }
                    catch (Exception e) when (handleGeneratorException(compilation, MessageProvider, sourceGenerator, e, isInit: true))
                    {
                        ex = e;
                    }

                    var outputNodes = outputBuilder.ToImmutableAndFree();
                    var inputNodes = inputBuilder.ToImmutableAndFree();

                    // run post init
                    if (ex is null)
                    {
                        try
                        {
                            IncrementalExecutionContext context = UpdateOutputs(outputNodes, IncrementalGeneratorOutputKind.PostInit, new GeneratorRunStateTable.Builder(false), cancellationToken);
                            postInitSources = ParseAdditionalSources(sourceGenerator, context.ToImmutableAndFree().sources, cancellationToken);
                        }
                        catch (UserFunctionException e) when (handleGeneratorException(compilation, MessageProvider, sourceGenerator, e, isInit: true))
                        {
                            ex = e.InnerException;
                        }
                    }

                    generatorState = ex is null
                                     ? new GeneratorState(postInitSources, inputNodes, outputNodes)
                                     : SetGeneratorException(compilation, MessageProvider, GeneratorState.Empty, sourceGenerator, ex, diagnosticsBag, cancellationToken, isInit: true);
                }
                else if (state.ParseOptionsChanged && generatorState.PostInitTrees.Length > 0)
                {
                    // the generator is initialized, but we need to reparse the post-init trees as the parse options have changed
                    var reparsedInitSources = ParseAdditionalSources(sourceGenerator, generatorState.PostInitTrees.SelectAsArray(t => new GeneratedSourceText(t.HintName, t.Text)), cancellationToken);
                    generatorState = new GeneratorState(reparsedInitSources, generatorState.InputNodes, generatorState.OutputNodes);
                }

                // if the pipeline registered any syntax input nodes, record them
                if (!generatorState.InputNodes.IsEmpty)
                {
                    syntaxInputNodes.AddRange(generatorState.InputNodes);
                }

                // record any constant sources
                if (generatorState.PostInitTrees.Length > 0)
                {
                    constantSourcesBuilder.AddRange(generatorState.PostInitTrees.Select(t => t.Tree));
                }

                stateBuilder.Add(generatorState);
            }

            // update the compilation with any constant sources
            if (constantSourcesBuilder.Count > 0)
            {
                compilation = compilation.AddSyntaxTrees(constantSourcesBuilder);
            }
            constantSourcesBuilder.Free();

            var syntaxStoreBuilder = _state.SyntaxStore.ToBuilder(compilation, syntaxInputNodes.ToImmutableAndFree(), _state.TrackIncrementalSteps, cancellationToken);

            var driverStateBuilder = new DriverStateTable.Builder(compilation, _state, syntaxStoreBuilder, cancellationToken);
            for (int i = 0; i < state.IncrementalGenerators.Length; i++)
            {
                var generatorState = stateBuilder[i];
                if (generatorState.OutputNodes.Length == 0)
                {
                    continue;
                }

                using var generatorTimer = CodeAnalysisEventSource.Log.CreateSingleGeneratorRunTimer(state.Generators[i], (t) => t.Add(syntaxStoreBuilder.GetRuntimeAdjustment(stateBuilder[i].InputNodes)));
                try
                {
                    // We do not support incremental step tracking for v1 generators, as the pipeline is implicitly defined.
                    var context = UpdateOutputs(generatorState.OutputNodes, IncrementalGeneratorOutputKind.Source | IncrementalGeneratorOutputKind.Implementation | HostKind, new GeneratorRunStateTable.Builder(state.TrackIncrementalSteps), cancellationToken, driverStateBuilder);
                    (var sources, var generatorDiagnostics, var generatorRunStateTable, var hostOutputs) = context.ToImmutableAndFree();
                    generatorDiagnostics = FilterDiagnostics(compilation, generatorDiagnostics, driverDiagnostics: diagnosticsBag, cancellationToken);

                    stateBuilder[i] = generatorState.WithResults(ParseAdditionalSources(state.Generators[i], sources, cancellationToken), generatorDiagnostics, generatorRunStateTable.ExecutedSteps, generatorRunStateTable.OutputSteps, hostOutputs, generatorTimer.Elapsed);
                }
                catch (UserFunctionException ufe) when (handleGeneratorException(compilation, MessageProvider, state.Generators[i], ufe.InnerException, isInit: false))
                {
                    stateBuilder[i] = SetGeneratorException(compilation, MessageProvider, generatorState, state.Generators[i], ufe.InnerException, diagnosticsBag, cancellationToken, runTime: generatorTimer.Elapsed);
                }
            }

            state = state.With(stateTable: driverStateBuilder.ToImmutable(), syntaxStore: syntaxStoreBuilder.ToImmutable(), generatorStates: stateBuilder.ToImmutableAndFree(), runTime: timer.Elapsed, parseOptionsChanged: false);
            return state;

            static bool handleGeneratorException(Compilation compilation, CommonMessageProvider messageProvider, ISourceGenerator sourceGenerator, Exception e, bool isInit)
            {
                if (!compilation.CatchAnalyzerExceptions)
                {
                    Debug.Assert(false);
                    Environment.FailFast(CreateGeneratorExceptionDiagnostic(messageProvider, sourceGenerator, e, isInit).ToString());
                    return false;
                }

                return true;
            }
        }

        private IncrementalExecutionContext UpdateOutputs(ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, IncrementalGeneratorOutputKind outputKind, GeneratorRunStateTable.Builder generatorRunStateBuilder, CancellationToken cancellationToken, DriverStateTable.Builder? driverStateBuilder = null)
        {
            Debug.Assert(outputKind != IncrementalGeneratorOutputKind.None);
            IncrementalExecutionContext context = new IncrementalExecutionContext(driverStateBuilder, generatorRunStateBuilder, new AdditionalSourcesCollection(SourceExtension));
            foreach (var outputNode in outputNodes)
            {
                // if we're looking for this output kind, and it has not been explicitly disabled
                if (outputKind.HasFlag(outputNode.Kind) && !_state.DisabledOutputs.HasFlag(outputNode.Kind))
                {
                    outputNode.AppendOutputs(context, cancellationToken);
                }
            }
            return context;
        }

        private ImmutableArray<GeneratedSyntaxTree> ParseAdditionalSources(ISourceGenerator generator, ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = ArrayBuilder<GeneratedSyntaxTree>.GetInstance(generatedSources.Length);
            var prefix = GetFilePathPrefixForGenerator(this._state.BaseDirectory, generator);
            foreach (var source in generatedSources)
            {
                var tree = ParseGeneratedSourceText(source, Path.Combine(prefix, source.HintName), cancellationToken);
                trees.Add(new GeneratedSyntaxTree(source.HintName, source.Text, tree));
            }
            return trees.ToImmutableAndFree();
        }

        private static GeneratorState SetGeneratorException(Compilation compilation, CommonMessageProvider provider, GeneratorState generatorState, ISourceGenerator generator, Exception e, DiagnosticBag? diagnosticBag, CancellationToken cancellationToken, TimeSpan? runTime = null, bool isInit = false)
        {
            if (CodeAnalysisEventSource.Log.IsEnabled())
            {
                CodeAnalysisEventSource.Log.GeneratorException(generator.GetGeneratorType().Name, e.ToString());
            }

            var diagnostic = CreateGeneratorExceptionDiagnostic(provider, generator, e, isInit);
            var filtered = compilation.Options.FilterDiagnostic(diagnostic, cancellationToken);

            if (filtered is not null)
            {
                diagnosticBag?.Add(filtered);
                return generatorState.WithError(e, filtered, runTime ?? TimeSpan.Zero);
            }
            return generatorState;
        }

        private static Diagnostic CreateGeneratorExceptionDiagnostic(CommonMessageProvider provider, ISourceGenerator generator, Exception e, bool isInit)
        {
            var errorCode = isInit ? provider.WRN_GeneratorFailedDuringInitialization : provider.WRN_GeneratorFailedDuringGeneration;

            // ISSUE: We should not call `e.CreateDiagnosticDescription()`, and instead pass formattable parts like `StackTrace`.
            // ISSUE: Exceptions also don't support IFormattable, so will always be in the current UI Culture.
            // ISSUE: See https://github.com/dotnet/roslyn/issues/46939

            var descriptor = new DiagnosticDescriptor(
                provider.GetIdForErrorCode(errorCode),
                provider.GetTitle(errorCode),
                provider.GetMessageFormat(errorCode),
                category: "Compiler",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);

            return Diagnostic.Create(descriptor, Location.None, generator.GetGeneratorType().Name, e.GetType().Name, e.Message, e.CreateDiagnosticDescription());
        }

        private static ImmutableArray<Diagnostic> FilterDiagnostics(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics, DiagnosticBag? driverDiagnostics, CancellationToken cancellationToken)
        {
            if (generatorDiagnostics.IsEmpty)
            {
                return generatorDiagnostics;
            }

            var suppressMessageState = new SuppressMessageAttributeState(compilation);
            ArrayBuilder<Diagnostic> filteredDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();
            foreach (var diag in generatorDiagnostics)
            {
                if (compilation.Options.FilterDiagnostic(diag, cancellationToken) is { } filtered &&
                    suppressMessageState.ApplySourceSuppressions(filtered) is { } effective)
                {
                    filteredDiagnostics.Add(effective);
                    driverDiagnostics?.Add(effective);
                }
            }
            return filteredDiagnostics.ToImmutableAndFree();
        }

        internal static string GetFilePathPrefixForGenerator(string? baseDirectory, ISourceGenerator generator)
        {
            var type = generator.GetGeneratorType();
            return Path.Combine(baseDirectory ?? "", type.Assembly.GetName().Name ?? string.Empty, type.FullName!);
        }

        private static ImmutableArray<IIncrementalGenerator> GetIncrementalGenerators(ImmutableArray<ISourceGenerator> generators, string sourceExtension)
        {
            return generators.SelectAsArray(g => g switch
            {
                IncrementalGeneratorWrapper igw => igw.Generator,
                IIncrementalGenerator ig => ig,
                _ => new SourceGeneratorAdaptor(g, sourceExtension)
            });

        }

        internal abstract CommonMessageProvider MessageProvider { get; }

        internal abstract GeneratorDriver FromState(GeneratorDriverState state);

        internal abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, string fileName, CancellationToken cancellationToken);

        internal abstract string SourceExtension { get; }

        internal abstract ISyntaxHelper SyntaxHelper { get; }
    }
}
