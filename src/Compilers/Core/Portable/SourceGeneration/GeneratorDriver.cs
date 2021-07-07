﻿// Licensed to the .NET Foundation under one or more agreements.
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
        internal readonly GeneratorDriverState _state;

        internal GeneratorDriver(GeneratorDriverState state)
        {
            Debug.Assert(state.Generators.GroupBy(s => s.GetGeneratorType()).Count() == state.Generators.Length); // ensure we don't have duplicate generator types
            _state = state;
        }

        internal GeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts, bool enableIncremental)
        {
            (var filteredGenerators, var incrementalGenerators) = GetIncrementalGenerators(generators, enableIncremental);
            _state = new GeneratorDriverState(parseOptions, optionsProvider, filteredGenerators, incrementalGenerators, additionalTexts, ImmutableArray.Create(new GeneratorState[filteredGenerators.Length]), DriverStateTable.Empty, enableIncremental);
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
            (var filteredGenerators, var incrementalGenerators) = GetIncrementalGenerators(generators, _state.EnableIncremental);
            var newState = _state.With(sourceGenerators: _state.Generators.AddRange(filteredGenerators),
                                       incrementalGenerators: _state.IncrementalGenerators.AddRange(incrementalGenerators),
                                       generatorStates: _state.GeneratorStates.AddRange(new GeneratorState[filteredGenerators.Length]));
            return FromState(newState);
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

        public GeneratorDriver WithUpdatedParseOptions(ParseOptions newOptions) => newOptions is object
                                                                                   ? FromState(_state.With(parseOptions: newOptions))
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
                                                          generatedSources: getGeneratorSources(generatorState)));
            return new GeneratorDriverRunResult(results);

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

        internal GeneratorDriverState RunGeneratorsCore(Compilation compilation, DiagnosticBag? diagnosticsBag, CancellationToken cancellationToken = default)
        {
            // with no generators, there is no work to do
            if (_state.Generators.IsEmpty)
            {
                return _state.With(stateTable: DriverStateTable.Empty);
            }

            // run the actual generation
            var state = _state;
            var stateBuilder = ArrayBuilder<GeneratorState>.GetInstance(state.Generators.Length);
            var constantSourcesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            var syntaxInputNodes = ArrayBuilder<ISyntaxInputNode>.GetInstance();

            for (int i = 0; i < state.IncrementalGenerators.Length; i++)
            {
                var generator = state.IncrementalGenerators[i];
                var generatorState = state.GeneratorStates[i];
                var sourceGenerator = state.Generators[i];

                // initialize the generator if needed
                if (!generatorState.Initialized)
                {
                    var outputBuilder = ArrayBuilder<IIncrementalGeneratorOutputNode>.GetInstance();
                    var inputBuilder = ArrayBuilder<ISyntaxInputNode>.GetInstance();
                    var postInitSources = ImmutableArray<GeneratedSyntaxTree>.Empty;
                    var pipelineContext = new IncrementalGeneratorInitializationContext(inputBuilder, outputBuilder);

                    Exception? ex = null;
                    try
                    {
                        generator.Initialize(pipelineContext);
                    }
                    catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
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
                            IncrementalExecutionContext context = UpdateOutputs(outputNodes, IncrementalGeneratorOutputKind.PostInit, cancellationToken);
                            postInitSources = ParseAdditionalSources(sourceGenerator, context.ToImmutableAndFree().sources, cancellationToken);
                        }
                        catch (UserFunctionException e)
                        {
                            ex = e.InnerException;
                        }
                    }

                    generatorState = ex is null
                                     ? new GeneratorState(generatorState.Info, postInitSources, inputNodes, outputNodes)
                                     : SetGeneratorException(MessageProvider, generatorState, sourceGenerator, ex, diagnosticsBag, isInit: true);
                }

                // if the pipeline registered any syntax input nodes, record them
                if (!generatorState.InputNodes.IsEmpty)
                {
                    syntaxInputNodes.AddRange(generatorState.InputNodes);
                }

                // record any constant sources
                if (generatorState.Exception is null && generatorState.PostInitTrees.Length > 0)
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

            var driverStateBuilder = new DriverStateTable.Builder(compilation, _state, syntaxInputNodes.ToImmutableAndFree(), cancellationToken);
            for (int i = 0; i < state.IncrementalGenerators.Length; i++)
            {
                var generatorState = stateBuilder[i];
                if (generatorState.Exception is object)
                {
                    continue;
                }

                try
                {
                    var context = UpdateOutputs(generatorState.OutputNodes, IncrementalGeneratorOutputKind.Source, cancellationToken, driverStateBuilder);
                    (var sources, var generatorDiagnostics) = context.ToImmutableAndFree();
                    generatorDiagnostics = FilterDiagnostics(compilation, generatorDiagnostics, driverDiagnostics: diagnosticsBag, cancellationToken);

                    stateBuilder[i] = new GeneratorState(generatorState.Info, generatorState.PostInitTrees, generatorState.InputNodes, generatorState.OutputNodes, ParseAdditionalSources(state.Generators[i], sources, cancellationToken), generatorDiagnostics);
                }
                catch (UserFunctionException ufe)
                {
                    stateBuilder[i] = SetGeneratorException(MessageProvider, stateBuilder[i], state.Generators[i], ufe.InnerException, diagnosticsBag);
                }
            }

            state = state.With(stateTable: driverStateBuilder.ToImmutable(), generatorStates: stateBuilder.ToImmutableAndFree());
            return state;
        }

        private IncrementalExecutionContext UpdateOutputs(ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, IncrementalGeneratorOutputKind outputKind, CancellationToken cancellationToken, DriverStateTable.Builder? driverStateBuilder = null)
        {
            IncrementalExecutionContext context = new IncrementalExecutionContext(driverStateBuilder, CreateSourcesCollection());
            foreach (var outputNode in outputNodes)
            {
                if (outputNode.Kind == outputKind)
                {
                    outputNode.AppendOutputs(context, cancellationToken);
                }
            }
            return context;
        }

        private ImmutableArray<GeneratedSyntaxTree> ParseAdditionalSources(ISourceGenerator generator, ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = ArrayBuilder<GeneratedSyntaxTree>.GetInstance(generatedSources.Length);
            var type = generator.GetGeneratorType();
            var prefix = GetFilePathPrefixForGenerator(generator);
            foreach (var source in generatedSources)
            {
                var tree = ParseGeneratedSourceText(source, Path.Combine(prefix, source.HintName), cancellationToken);
                trees.Add(new GeneratedSyntaxTree(source.HintName, source.Text, tree));
            }
            return trees.ToImmutableAndFree();
        }

        private static GeneratorState SetGeneratorException(CommonMessageProvider provider, GeneratorState generatorState, ISourceGenerator generator, Exception e, DiagnosticBag? diagnosticBag, bool isInit = false)
        {
            var errorCode = isInit ? provider.WRN_GeneratorFailedDuringInitialization : provider.WRN_GeneratorFailedDuringGeneration;

            // ISSUE: Diagnostics don't currently allow descriptions with arguments, so we have to manually create the diagnostic description
            // ISSUE: Exceptions also don't support IFormattable, so will always be in the current UI Culture.
            // ISSUE: See https://github.com/dotnet/roslyn/issues/46939

            var description = string.Format(provider.GetDescription(errorCode).ToString(CultureInfo.CurrentUICulture), e);

            var descriptor = new DiagnosticDescriptor(
                provider.GetIdForErrorCode(errorCode),
                provider.GetTitle(errorCode),
                provider.GetMessageFormat(errorCode),
                description: description,
                category: "Compiler",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);

            var diagnostic = Diagnostic.Create(descriptor, Location.None, generator.GetGeneratorType().Name, e.GetType().Name, e.Message);

            diagnosticBag?.Add(diagnostic);
            return new GeneratorState(generatorState.Info, e, diagnostic);
        }

        private static ImmutableArray<Diagnostic> FilterDiagnostics(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics, DiagnosticBag? driverDiagnostics, CancellationToken cancellationToken)
        {
            ArrayBuilder<Diagnostic> filteredDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();
            foreach (var diag in generatorDiagnostics)
            {
                var filtered = compilation.Options.FilterDiagnostic(diag, cancellationToken);
                if (filtered is object)
                {
                    filteredDiagnostics.Add(filtered);
                    driverDiagnostics?.Add(filtered);
                }
            }
            return filteredDiagnostics.ToImmutableAndFree();
        }

        internal static string GetFilePathPrefixForGenerator(ISourceGenerator generator)
        {
            var type = generator.GetGeneratorType();
            return Path.Combine(type.Assembly.GetName().Name ?? string.Empty, type.FullName!);
        }

        private static (ImmutableArray<ISourceGenerator>, ImmutableArray<IIncrementalGenerator>) GetIncrementalGenerators(ImmutableArray<ISourceGenerator> generators, bool enableIncremental)
        {
            if (enableIncremental)
            {
                return (generators, generators.SelectAsArray(g => g switch
                {
                    IncrementalGeneratorWrapper igw => igw.Generator,
                    IIncrementalGenerator ig => ig,
                    _ => new SourceGeneratorAdaptor(g)
                }));
            }
            else
            {
                var filtered = generators.WhereAsArray(g => g is not IncrementalGeneratorWrapper);
                return (filtered, filtered.SelectAsArray<ISourceGenerator, IIncrementalGenerator>(g => new SourceGeneratorAdaptor(g)));
            }
        }

        internal abstract CommonMessageProvider MessageProvider { get; }

        internal abstract GeneratorDriver FromState(GeneratorDriverState state);

        internal abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, string fileName, CancellationToken cancellationToken);

        internal abstract AdditionalSourcesCollection CreateSourcesCollection();
    }
}
