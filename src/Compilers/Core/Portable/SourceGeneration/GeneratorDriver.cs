// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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

        public GeneratorDriver RunFullGeneration(Compilation compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken = default)
        {
            // with no generators, there is no work to do
            if (_state.Generators.Length == 0)
            {
                outputCompilation = compilation;
                diagnostics = ImmutableArray<Diagnostic>.Empty;
                return this;
            }

            // run the actual generation
            var state = StateWithPendingEditsApplied(_state);
            var stateBuilder = ArrayBuilder<GeneratorState>.GetInstance();
            var receivers = PooledDictionary<ISourceGenerator, ISyntaxReceiver>.GetInstance();
            var diagnosticsBag = new DiagnosticBag();

            for (int i = 0; i < state.Generators.Length; i++)
            {
                var generator = state.Generators[i];
                var generatorState = state.GeneratorStates[i];

                // initialize the generator if needed
                if (!generatorState.Info.Initialized)
                {
                    generatorState = InitializeGenerator(generator, diagnosticsBag, cancellationToken);
                }
                stateBuilder.Add(generatorState);

                // create the syntax receiver if requested
                if (generatorState.Info.SyntaxReceiverCreator is object)
                {
                    var rx = generatorState.Info.SyntaxReceiverCreator();
                    receivers.Add(generator, rx);
                }
            }

            // Run a syntax walk if any of the generators requested it
            if (receivers.Count > 0)
            {
                GeneratorSyntaxWalker walker = new GeneratorSyntaxWalker(receivers.Values.ToImmutableArray());
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    walker.Visit(syntaxTree.GetRoot(cancellationToken));
                }
            }

            // https://github.com/dotnet/roslyn/issues/42629: should be possible to parallelize this
            for (int i = 0; i < state.Generators.Length; i++)
            {
                var generator = state.Generators[i];
                var generatorState = stateBuilder[i];
                try
                {
                    // don't try and generate if initialization failed
                    if (!generatorState.Info.Initialized)
                    {
                        continue;
                    }

                    // we create a new context for each run of the generator. We'll never re-use existing state, only replace anything we have
                    _ = receivers.TryGetValue(generator, out var syntaxReceiverOpt);
                    var context = new SourceGeneratorContext(compilation, state.AdditionalTexts.NullToEmpty(), state.OptionsProvider, syntaxReceiverOpt, diagnosticsBag);
                    generator.Execute(context);
                    stateBuilder[i] = generatorState.WithSources(ParseAdditionalSources(generator, context.AdditionalSources.ToImmutableAndFree(), cancellationToken));
                }
                catch
                {
                    diagnosticsBag.Add(Diagnostic.Create(MessageProvider, MessageProvider.WRN_GeneratorFailedDuringGeneration, generator.GetType().Name));
                }
            }
            state = state.With(generatorStates: stateBuilder.ToImmutableAndFree());
            diagnostics = diagnosticsBag.ToReadOnlyAndFree();

            // build the final state, and return 
            return BuildFinalCompilation(compilation, out outputCompilation, state, cancellationToken);
        }

        public GeneratorDriver TryApplyEdits(Compilation compilation, out Compilation outputCompilation, out bool success, CancellationToken cancellationToken = default)
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
                    var context = new EditContext(generatorState.Sources.Keys.ToImmutableArray(), cancellationToken);
                    var succeeded = edit.TryApply(generatorState.Info, context);
                    if (!succeeded)
                    {
                        // we couldn't successfully apply this edit
                        // return the original state noting we failed
                        return initialState.With(editsFailed: true);
                    }

                    // update the state with the new edits
                    state = state.With(generatorStates: state.GeneratorStates.SetItem(i, generatorState.WithSources(ParseAdditionalSources(generator, context.AdditionalSources.ToImmutableAndFree(), cancellationToken))));
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

        private GeneratorState InitializeGenerator(ISourceGenerator generator, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            GeneratorInfo info = default;
            try
            {
                InitializationContext context = new InitializationContext(cancellationToken);
                generator.Initialize(context);
                info = context.InfoBuilder.ToImmutable();
            }
            catch
            {
                diagnostics.Add(Diagnostic.Create(MessageProvider, MessageProvider.WRN_GeneratorFailedDuringInitialization, generator.GetType().Name));
            }
            return new GeneratorState(info);
        }

        private static Compilation RemoveGeneratedSyntaxTrees(GeneratorDriverState state, Compilation compilation)
        {
            ArrayBuilder<SyntaxTree> trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var generatorState in state.GeneratorStates)
            {
                foreach (var (_, tree) in generatorState.Sources)
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

        private ImmutableDictionary<GeneratedSourceText, SyntaxTree> ParseAdditionalSources(ISourceGenerator generator, ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = PooledDictionary<GeneratedSourceText, SyntaxTree>.GetInstance();
            var type = generator.GetType();
            var prefix = $"{type.Module.ModuleVersionId}_{type.FullName}";
            foreach (var source in generatedSources)
            {
                trees.Add(source, ParseGeneratedSourceText(source, $"{prefix}_{source.HintName}", cancellationToken));
            }
            return trees.ToImmutableDictionaryAndFree();
        }

        private GeneratorDriver BuildFinalCompilation(Compilation compilation, out Compilation outputCompilation, GeneratorDriverState state, CancellationToken cancellationToken)
        {
            ArrayBuilder<SyntaxTree> trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var generatorState in state.GeneratorStates)
            {
                trees.AddRange(generatorState.Sources.Values);
            }
            outputCompilation = compilation.AddSyntaxTrees(trees);
            trees.Free();

            state = state.With(edits: ImmutableArray<PendingEdit>.Empty,
                               editsFailed: false);
            return FromState(state);
        }

        internal abstract CommonMessageProvider MessageProvider { get; }

        internal abstract GeneratorDriver FromState(GeneratorDriverState state);

        internal abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, string fileName, CancellationToken cancellationToken);
    }
}
