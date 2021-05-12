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
            Debug.Assert(state.Generators.GroupBy(s => s.GetType()).Count() == state.Generators.Length); // ensure we don't have duplicate generator types
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

        /// <summary>
        /// Returns the underlying type of a given generator
        /// </summary>
        /// <remarks>
        /// For <see cref="IIncrementalGenerator"/>s we create a wrapper type that also implements
        /// <see cref="ISourceGenerator"/>. This method will unwrap and return the underlying type
        /// in those cases.
        /// </remarks>
        /// <param name="generator">The generator to get the type of</param>
        /// <returns>The underlying generator type</returns>
        public static Type GetGeneratorType(ISourceGenerator generator)
        {
            if (generator is IncrementalGeneratorWrapper igw)
            {
                return igw.Generator.GetType();
            }
            return generator.GetType();
        }

        /// <summary>
        /// Wraps an <see cref="IIncrementalGenerator"/> in an <see cref="ISourceGenerator"/> object that can be used to construct a <see cref="GeneratorDriver"/>
        /// </summary>
        /// <param name="incrementalGenerator">The incremental generator to wrap</param>
        /// <returns>A wrapped generator that can be passed to a generator driver</returns>
        public static ISourceGenerator WrapGenerator(IIncrementalGenerator incrementalGenerator) => new IncrementalGeneratorWrapper(incrementalGenerator);

        internal GeneratorDriverState RunGeneratorsCore(Compilation compilation, DiagnosticBag? diagnosticsBag, CancellationToken cancellationToken = default)
        {
            // with no generators, there is no work to do
            if (_state.Generators.IsEmpty)
            {
                return _state;
            }

            // run the actual generation
            var state = _state;
            var stateBuilder = ArrayBuilder<GeneratorState>.GetInstance(state.Generators.Length);
            var constantSourcesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            var walkerBuilder = ArrayBuilder<GeneratorSyntaxWalker?>.GetInstance(state.Generators.Length, fillWithValue: null); // we know there is at max 1 per generator
            var syntaxInputNodes = ArrayBuilder<ISyntaxTransformNode>.GetInstance();
            int walkerCount = 0;

            for (int i = 0; i < state.IncrementalGenerators.Length; i++)
            {
                var generator = state.IncrementalGenerators[i];
                var generatorState = state.GeneratorStates[i];
                var sourceGenerator = state.Generators[i];

                // initialize the generator if needed
                if (!generatorState.Info.Initialized)
                {
                    var context = new IncrementalGeneratorInitializationContext(cancellationToken);
                    Exception? ex = null;
                    try
                    {
                        generator.Initialize(context);
                    }
                    catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                    {
                        ex = e;
                    }
                    generatorState = ex is null
                                     ? new GeneratorState(context.InfoBuilder.ToImmutable())
                                     : SetGeneratorException(MessageProvider, GeneratorState.Uninitialized, sourceGenerator, ex, diagnosticsBag, isInit: true);

                    // invoke the post init callback if requested
                    if (generatorState.Info.PostInitCallback is object)
                    {
                        var sourcesCollection = this.CreateSourcesCollection();
                        var postContext = new GeneratorPostInitializationContext(sourcesCollection, cancellationToken);
                        try
                        {
                            generatorState.Info.PostInitCallback(postContext);
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        generatorState = ex is null
                                         ? new GeneratorState(generatorState.Info, ParseAdditionalSources(sourceGenerator, sourcesCollection.ToImmutable(), cancellationToken))
                                         : SetGeneratorException(MessageProvider, generatorState, sourceGenerator, ex, diagnosticsBag, isInit: true);

                        sourcesCollection.Free();
                    }

                    // create the execution pipeline
                    if (ex is null && generatorState.Info.PipelineCallback is object)
                    {
                        var outputBuilder = ArrayBuilder<IIncrementalGeneratorOutputNode>.GetInstance();
                        var sourcesBuilder = new PerGeneratorInputNodes.Builder();
                        var pipelineContext = new IncrementalGeneratorPipelineContext(sourcesBuilder, outputBuilder);
                        try
                        {
                            generatorState.Info.PipelineCallback(pipelineContext);
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }

                        generatorState = ex is null
                                         ? new GeneratorState(generatorState.Info, generatorState.PostInitTrees, sourcesBuilder.ToImmutable(), outputBuilder.ToImmutable())
                                         : SetGeneratorException(MessageProvider, generatorState, sourceGenerator, ex, diagnosticsBag, isInit: true);

                        outputBuilder.Free();
                        sourcesBuilder.Free();
                    }
                }

                // create the syntax receiver if requested
                if (generatorState.Info.SyntaxContextReceiverCreator is object && generatorState.Exception is null)
                {
                    ISyntaxContextReceiver? rx = null;
                    try
                    {
                        rx = generatorState.Info.SyntaxContextReceiverCreator();
                    }
                    catch (Exception e)
                    {
                        generatorState = SetGeneratorException(MessageProvider, generatorState, sourceGenerator, e, diagnosticsBag);
                    }

                    if (rx is object)
                    {
                        walkerBuilder.SetItem(i, new GeneratorSyntaxWalker(rx));
                        generatorState = generatorState.WithReceiver(rx);
                        walkerCount++;
                    }
                }

                // if the pipeline registered any syntax callbacks, record them in the syntax input node list
                if (!generatorState.Sources.TransformNodes.IsEmpty)
                {
                    // it isn't possible for an incremental generator to directly create a walker as part of init
                    Debug.Assert(generatorState.Info.SyntaxContextReceiverCreator is null);
                    syntaxInputNodes.AddRange(generatorState.Sources.TransformNodes);
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


            // PROTOTYPE(source-generators): we don't need to run at all if none of the inputs have changes.
            var driverStateBuilder = new DriverStateTable.Builder(_state.StateTable, cancellationToken);

            driverStateBuilder.AddInput(SharedInputNodes.Compilation, compilation);
            driverStateBuilder.AddInput(SharedInputNodes.SyntaxTrees, compilation.SyntaxTrees);
            driverStateBuilder.AddInput(SharedInputNodes.AnalyzerConfigOptions, _state.OptionsProvider);
            driverStateBuilder.AddInput(SharedInputNodes.ParseOptions, _state.ParseOptions);
            driverStateBuilder.AddInput(SharedInputNodes.AdditionalTexts, _state.AdditionalTexts);

            // PROTOTYPE(source-generators): we should probably just pass these at creation, and need to handle syntax rx's too
            driverStateBuilder.AddSyntaxNodes(syntaxInputNodes.ToImmutable());

            // If we have syntax inputs, or receivers, bring them up to date
            if (syntaxInputNodes.Count > 0 || walkerCount > 0)
            {
                var builders = ArrayBuilder<ISyntaxTransformBuilder>.GetInstance(syntaxInputNodes.Count);
                foreach (var node in syntaxInputNodes)
                {
                   // builders.Add(node.GetBuilder(_state.StateTable));
                }

                foreach ((var tree, var treeState) in driverStateBuilder.GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees))
                {
                    if (treeState == EntryState.Removed)
                    {
                        // we need to keep the removed tree entries, but can skip everything else
                        foreach (var b in builders)
                        {
                          //  b.AddFilterFromPreviousTable(null, EntryState.Removed);
                        }
                        continue;
                    }

                    // PROTOTYPE(source-generators): It would be nice to somehow collect all the results up front, then pass them to the builder
                    //                               so we don't have to worry about the generic and have the extra interface.

                    var semanticModel = compilation.GetSemanticModel(tree);
                    if (treeState == EntryState.Cached)
                    {
                        foreach (var b in builders)
                        {
                           // b.AddFilterFromPreviousTable(semanticModel, treeState);
                        }
                    }
                    else
                    {
                        // run a walk for each filter and apply the results
                        //IncrementalGeneratorSyntaxWalker.VisitNodeForBuilders(tree.GetRoot(cancellationToken), semanticModel, builders);
                    }

                    // back compat, run this tree over any ISyntaxContextReceivers
                    if (walkerCount > 0)
                    {
                        var root = tree.GetRoot(cancellationToken);
                        for (int i = 0; i < walkerBuilder.Count; i++)
                        {
                            var walker = walkerBuilder[i];
                            if (walker is object)
                            {
                                try
                                {
                                    walker.VisitWithModel(semanticModel, root);
                                }
                                catch (Exception e)
                                {
                                    stateBuilder[i] = SetGeneratorException(MessageProvider, stateBuilder[i], state.Generators[i], e, diagnosticsBag);
                                    walkerBuilder.SetItem(i, null); // don't re-visit this walker for any other trees
                                }
                            }
                        }
                    }
                }

                // add the updated nodes as inputs to the new graph
                foreach (var builder in builders)
                {
                    //builder.AddInputs(driverStateBuilder);
                }

                builders.Free();
            }
            walkerBuilder.Free();

            for (int i = 0; i < state.IncrementalGenerators.Length; i++)
            {
                var generatorState = stateBuilder[i];
                if (generatorState.Exception is object)
                {
                    continue;
                }

                if (generatorState.Sources.ReceiverNode is object)
                {
                    driverStateBuilder.AddInput(generatorState.Sources.ReceiverNode, generatorState.SyntaxReceiver);
                }

                IncrementalExecutionContext context = new IncrementalExecutionContext(driverStateBuilder, CreateSourcesCollection());
                try
                {
                    foreach (var output in generatorState.OutputNodes)
                    {
                        // PROTOTYPE(source-generators):
                        // right now, we always run all output types. We'll add a mechanism to allow the host
                        // to control what types they care about in the future
                        output.AppendOutputs(context);
                    }

                    (var sources, var generatorDiagnostics) = context.ToImmutableAndFree();
                    generatorDiagnostics = FilterDiagnostics(compilation, generatorDiagnostics, driverDiagnostics: diagnosticsBag, cancellationToken);

                    stateBuilder[i] = new GeneratorState(generatorState.Info, generatorState.PostInitTrees, generatorState.Sources, generatorState.OutputNodes, ParseAdditionalSources(state.Generators[i], sources, cancellationToken), generatorDiagnostics);
                }
                catch (UserFunctionException ufe)
                {
                    stateBuilder[i] = SetGeneratorException(MessageProvider, stateBuilder[i], state.Generators[i], ufe.InnerException, diagnosticsBag);
                    context.Free();
                }
            }

            state = state.With(stateTable: driverStateBuilder.ToImmutable(), generatorStates: stateBuilder.ToImmutableAndFree());
            return state;
        }

        private ImmutableArray<GeneratedSyntaxTree> ParseAdditionalSources(ISourceGenerator generator, ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = ArrayBuilder<GeneratedSyntaxTree>.GetInstance(generatedSources.Length);
            var type = GetGeneratorType(generator);
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

            var diagnostic = Diagnostic.Create(descriptor, Location.None, generator.GetType().Name, e.GetType().Name, e.Message);

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
            var type = GetGeneratorType(generator);
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
