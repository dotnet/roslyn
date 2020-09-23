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

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Responsible for orchestrating a source generation pass
    /// </summary>
    /// <remarks>
    /// GeneratorDriver is an immutable class that can be manipulated by returning a mutated copy of itself.
    /// In the compiler we only ever create a single instance and ignore the mutated copy. The IDE may perform 
    /// multiple edits, or generation passes of the same driver, re-using the state as needed.
    /// 
    /// A generator driver works like a small state machine:
    ///   - It starts off with no generated sources
    ///   - A full generation pass will run every generator and produce all possible generated source
    ///   - At any time an 'edit' maybe supplied, which represents potential future work
    ///   - TryApplyChanges can be called, which will iterate through the pending edits and try and attempt to 
    ///     bring the state back to what it would be if a full generation occurred by running partial generation
    ///     on generators that support it
    ///   - At any time a full generation pass can be re-run, resetting the pending edits
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
            _state = new GeneratorDriverState(parseOptions, optionsProvider, generators, additionalTexts, ImmutableArray.Create(new GeneratorState[generators.Length]), ImmutableArray<PendingEdit>.Empty, editsFailed: true);
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

            // build the final state, and return 
            diagnostics = diagnosticsBag.ToReadOnlyAndFree();
            return BuildFinalCompilation(compilation, out outputCompilation, state, cancellationToken);
        }

        internal GeneratorDriver TryApplyEdits(Compilation compilation, out Compilation outputCompilation, out bool success, CancellationToken cancellationToken = default)
        {
            // if we can't apply any partial edits, just instantly return
            if (_state.EditsFailed || _state.Edits.Length == 0)
            {
                outputCompilation = compilation;
                success = !_state.EditsFailed;
                return this;
            }

            // Apply any pending edits
            var state = _state;
            foreach (var edit in _state.Edits)
            {
                state = ApplyPartialEdit(state, edit, cancellationToken);
                if (state.EditsFailed)
                {
                    outputCompilation = compilation;
                    success = false;
                    return this;
                }
            }

            // remove the previously generated syntax trees
            compilation = RemoveGeneratedSyntaxTrees(_state, compilation);

            success = true;
            return BuildFinalCompilation(compilation, out outputCompilation, state, cancellationToken);
        }

        public GeneratorDriver AddGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            // set editsFailed true, as we won't be able to apply edits with a new generator
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
                                                          generatedSources: generatorState.SourceTexts.ZipAsArray(
                                                                                generatorState.Trees,
                                                                                (sourceText, tree) => new GeneratedSourceResult(tree, sourceText.Text, sourceText.HintName))));
            return new GeneratorDriverRunResult(results);
        }

        internal GeneratorDriverState RunGeneratorsCore(Compilation compilation, DiagnosticBag? diagnosticsBag, CancellationToken cancellationToken = default)
        {
            // with no generators, there is no work to do
            if (_state.Generators.IsEmpty)
            {
                return _state;
            }

            // run the actual generation
            var state = StateWithPendingEditsApplied(_state);
            var stateBuilder = ArrayBuilder<GeneratorState>.GetInstance(state.Generators.Length);
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
                    catch (Exception e)
                    {
                        ex = e;
                    }
                    generatorState = ex is null
                                     ? new GeneratorState(context.InfoBuilder.ToImmutable())
                                     : SetGeneratorException(MessageProvider, GeneratorState.Uninitialized, generator, ex, diagnosticsBag, isInit: true);
                }

                // create the syntax receiver if requested
                if (generatorState.Info.SyntaxReceiverCreator is object)
                {
                    try
                    {
                        var rx = generatorState.Info.SyntaxReceiverCreator();
                        walkerBuilder.SetItem(i, new GeneratorSyntaxWalker(rx));
                        generatorState = generatorState.WithReceiver(rx);
                        receiverCount++;
                    }
                    catch (Exception e)
                    {
                        generatorState = SetGeneratorException(MessageProvider, generatorState, generator, e, diagnosticsBag);
                    }
                }

                stateBuilder.Add(generatorState);
            }


            // Run a syntax walk if any of the generators requested it
            if (receiverCount > 0)
            {
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var root = tree.GetRoot(cancellationToken);

                    // https://github.com/dotnet/roslyn/issues/42629: should be possible to parallelize this
                    for (int i = 0; i < walkerBuilder.Count; i++)
                    {
                        var walker = walkerBuilder[i];
                        if (walker is object)
                        {
                            try
                            {
                                walker.Visit(root);
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
                var context = new GeneratorExecutionContext(compilation, state.ParseOptions, state.AdditionalTexts.NullToEmpty(), state.OptionsProvider, generatorState.SyntaxReceiver);
                try
                {
                    generator.Execute(context);
                }
                catch (Exception e)
                {
                    stateBuilder[i] = SetGeneratorException(MessageProvider, generatorState, generator, e, diagnosticsBag);
                    continue;
                }

                (var sources, var diagnostics) = context.ToImmutableAndFree();
                stateBuilder[i] = new GeneratorState(generatorState.Info, sources, ParseAdditionalSources(generator, sources, cancellationToken), diagnostics);
                diagnosticsBag?.AddRange(diagnostics);
            }
            state = state.With(generatorStates: stateBuilder.ToImmutableAndFree());
            return state;
        }

        // When we expose this publicly, remove arbitrary edit adding and replace with dedicated edit types
        internal GeneratorDriver WithPendingEdits(ImmutableArray<PendingEdit> edits)
        {
            var newState = _state.With(edits: _state.Edits.AddRange(edits));
            return FromState(newState);
        }

        private GeneratorDriverState ApplyPartialEdit(GeneratorDriverState state, PendingEdit edit, CancellationToken cancellationToken = default)
        {
            var initialState = state;

            // see if any generators accept this particular edit
            var stateBuilder = PooledDictionary<ISourceGenerator, GeneratorState>.GetInstance();
            for (int i = 0; i < initialState.Generators.Length; i++)
            {
                var generator = initialState.Generators[i];
                var generatorState = initialState.GeneratorStates[i];
                if (edit.AcceptedBy(generatorState.Info))
                {
                    // attempt to apply the edit
                    var context = new GeneratorEditContext(generatorState.SourceTexts.ToImmutableArray(), cancellationToken);
                    var succeeded = edit.TryApply(generatorState.Info, context);
                    if (!succeeded)
                    {
                        // we couldn't successfully apply this edit
                        // return the original state noting we failed
                        return initialState.With(editsFailed: true);
                    }

                    // update the state with the new edits
                    var additionalSources = context.AdditionalSources.ToImmutableAndFree();
                    state = state.With(generatorStates: state.GeneratorStates.SetItem(i, new GeneratorState(generatorState.Info, sourceTexts: additionalSources, trees: ParseAdditionalSources(generator, additionalSources, cancellationToken), diagnostics: ImmutableArray<Diagnostic>.Empty)));
                }
            }
            state = edit.Commit(state);
            return state;
        }

        private static GeneratorDriverState StateWithPendingEditsApplied(GeneratorDriverState state)
        {
            if (state.Edits.Length == 0)
            {
                return state;
            }

            foreach (var edit in state.Edits)
            {
                state = edit.Commit(state);
            }
            return state.With(edits: ImmutableArray<PendingEdit>.Empty, editsFailed: false);
        }

        private static Compilation RemoveGeneratedSyntaxTrees(GeneratorDriverState state, Compilation compilation)
        {
            ArrayBuilder<SyntaxTree> trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var generatorState in state.GeneratorStates)
            {
                foreach (var tree in generatorState.Trees)
                {
                    if (tree is object && compilation.ContainsSyntaxTree(tree))
                    {
                        trees.Add(tree);
                    }
                }
            }

            var comp = compilation.RemoveSyntaxTrees(trees);
            trees.Free();
            return comp;
        }

        private ImmutableArray<SyntaxTree> ParseAdditionalSources(ISourceGenerator generator, ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = ArrayBuilder<SyntaxTree>.GetInstance(generatedSources.Length);
            var type = generator.GetType();
            var prefix = GetFilePathPrefixForGenerator(generator);
            foreach (var source in generatedSources)
            {
                trees.Add(ParseGeneratedSourceText(source, Path.Combine(prefix, source.HintName), cancellationToken));
            }
            return trees.ToImmutableAndFree();
        }

        private GeneratorDriver BuildFinalCompilation(Compilation compilation, out Compilation outputCompilation, GeneratorDriverState state, CancellationToken cancellationToken)
        {
            ArrayBuilder<SyntaxTree> trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var generatorState in state.GeneratorStates)
            {
                trees.AddRange(generatorState.Trees);
            }
            outputCompilation = compilation.AddSyntaxTrees(trees);
            trees.Free();

            state = state.With(edits: ImmutableArray<PendingEdit>.Empty,
                               editsFailed: false);
            return FromState(state);
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
    }
}
