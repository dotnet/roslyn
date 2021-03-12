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

        internal GeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts)
        {
            _state = new GeneratorDriverState(parseOptions, optionsProvider, generators, additionalTexts, ImmutableArray.Create(new GeneratorState[generators.Length]));
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
            var newState = _state.With(generators: _state.Generators.AddRange(generators), generatorStates: _state.GeneratorStates.AddRange(new GeneratorState[generators.Length]), editsFailed: true);
            return FromState(newState);
        }

        public GeneratorDriver RemoveGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            var newGenerators = _state.Generators;
            var newStates = _state.GeneratorStates;
            for (int i = 0; i < newGenerators.Length; i++)
            {
                if (generators.Contains(newGenerators[i]))
                {
                    newGenerators = newGenerators.RemoveAt(i);
                    newStates = newStates.RemoveAt(i);
                    i--;
                }
            }

            return FromState(_state.With(generators: newGenerators, generatorStates: newStates));
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
            int receiverCount = 0;

            for (int i = 0; i < state.Generators.Length; i++)
            {
                var generator = state.Generators[i];
                var generatorState = state.GeneratorStates[i];

                // initialize the generator if needed
                if (!generatorState.Info.Initialized)
                {
                    var context = new GeneratorInitializationContext(cancellationToken);
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
                                     : SetGeneratorException(MessageProvider, GeneratorState.Uninitialized, generator, ex, diagnosticsBag, isInit: true);

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
                                         ? new GeneratorState(generatorState.Info, ParseAdditionalSources(generator, sourcesCollection.ToImmutableAndFree(), cancellationToken))
                                         : SetGeneratorException(MessageProvider, generatorState, generator, ex, diagnosticsBag, isInit: true);
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
                        generatorState = SetGeneratorException(MessageProvider, generatorState, generator, e, diagnosticsBag);
                    }

                    if (rx is object)
                    {
                        walkerBuilder.SetItem(i, new GeneratorSyntaxWalker(rx));
                        generatorState = generatorState.WithReceiver(rx);
                        receiverCount++;
                    }
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

            // Run a syntax walk if any of the generators requested it
            if (receiverCount > 0)
            {
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var root = tree.GetRoot(cancellationToken);
                    var semanticModel = compilation.GetSemanticModel(tree);

                    // https://github.com/dotnet/roslyn/issues/42629: should be possible to parallelize this
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
            walkerBuilder.Free();

            // https://github.com/dotnet/roslyn/issues/42629: should be possible to parallelize this
            for (int i = 0; i < state.Generators.Length; i++)
            {
                var generator = state.Generators[i];
                var generatorState = stateBuilder[i];

                // don't try and generate if initialization or syntax walk failed
                if (generatorState.Exception is object)
                {
                    continue;
                }
                Debug.Assert(generatorState.Info.Initialized);

                // we create a new context for each run of the generator. We'll never re-use existing state, only replace anything we have 
                var context = new GeneratorExecutionContext(compilation, state.ParseOptions, state.AdditionalTexts.NullToEmpty(), state.OptionsProvider, generatorState.SyntaxReceiver, CreateSourcesCollection(), cancellationToken);
                try
                {
                    generator.Execute(context);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    stateBuilder[i] = SetGeneratorException(MessageProvider, generatorState, generator, e, diagnosticsBag);
                    continue;
                }

                (var sources, var diagnostics) = context.ToImmutableAndFree();
                stateBuilder[i] = new GeneratorState(generatorState.Info, generatorState.PostInitTrees, ParseAdditionalSources(generator, sources, cancellationToken), diagnostics);
                diagnosticsBag?.AddRange(diagnostics);
            }
            state = state.With(generatorStates: stateBuilder.ToImmutableAndFree());
            return state;
        }

        private ImmutableArray<GeneratedSyntaxTree> ParseAdditionalSources(ISourceGenerator generator, ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = ArrayBuilder<GeneratedSyntaxTree>.GetInstance(generatedSources.Length);
            var type = generator.GetType();
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

        internal static string GetFilePathPrefixForGenerator(ISourceGenerator generator)
        {
            var type = generator.GetType();
            return Path.Combine(type.Assembly.GetName().Name ?? string.Empty, type.FullName!);
        }

        internal abstract CommonMessageProvider MessageProvider { get; }

        internal abstract GeneratorDriver FromState(GeneratorDriverState state);

        internal abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, string fileName, CancellationToken cancellationToken);

        internal abstract AdditionalSourcesCollection CreateSourcesCollection();
    }
}
