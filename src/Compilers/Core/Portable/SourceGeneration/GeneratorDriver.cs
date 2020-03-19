// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public abstract class GeneratorDriver
    {
        internal readonly GeneratorDriverState _state;

        internal GeneratorDriver(GeneratorDriverState state)
        {
            _state = state;
        }

        internal GeneratorDriver(Compilation compilation, ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, ImmutableArray<AdditionalText> additionalTexts)
        {
            _state = new GeneratorDriverState(compilation, parseOptions, generators, additionalTexts, ImmutableDictionary<ISourceGenerator, GeneratorState>.Empty, ImmutableArray<PendingEdit>.Empty, finalCompilation: null, editsFailed: true);
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
            var stateBuilder = PooledDictionary<ISourceGenerator, GeneratorState>.GetInstance();
            var receivers = PooledDictionary<ISourceGenerator, ISyntaxReceiver>.GetInstance();
            var diagnosticsBag = new DiagnosticBag();

            foreach (var generator in state.Generators)
            {
                // initialize the generator if needed
                if (!state.GeneratorStates.TryGetValue(generator, out GeneratorState generatorState))
                {
                    generatorState = InitializeGenerator(generator, diagnosticsBag, cancellationToken);
                }

                if (generatorState.Info.Initialized)
                {
                    stateBuilder.Add(generator, generatorState);
                }

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
                    walker.Visit(syntaxTree.GetRoot());
                }
            }

            //PROTOTYPE: should be possible to parallelize this
            foreach (var (generator, generatorState) in stateBuilder.ToImmutableArray())
            {
                try
                {
                    // we create a new context for each run of the generator. We'll never re-use existing state, only replace anything we have
                    _ = receivers.TryGetValue(generator, out var syntaxReceiverOpt);
                    var context = new SourceGeneratorContext(state.Compilation, new AnalyzerOptions(state.AdditionalTexts.NullToEmpty(), CompilerAnalyzerConfigOptionsProvider.Empty), syntaxReceiverOpt);
                    generator.Execute(context);
                    stateBuilder[generator] = generatorState.WithSources(ParseAdditionalSources(context.AdditionalSources.ToImmutableAndFree(), cancellationToken));
                }
                catch
                {
                    diagnosticsBag.Add(Diagnostic.Create(MessageProvider, MessageProvider.WRN_GeneratorFailedDuringGeneration, generator.GetType().Name));
                }
            }
            state = state.With(generatorStates: stateBuilder.ToImmutableDictionaryAndFree());
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
                // PROTOTYPE: we'll need to pass in the various compilation states too
                state = ApplyPartialEdit(state, edit);
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
            var newState = _state.With(generators: _state.Generators.AddRange(generators), editsFailed: true);
            return FromState(newState);
        }

        public GeneratorDriver RemoveGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            var newState = _state.With(generators: _state.Generators.RemoveRange(generators), generatorStates: _state.GeneratorStates.RemoveRange(generators));
            return FromState(newState);
        }

        public GeneratorDriver AddAdditionalTexts(ImmutableArray<AdditionalText> additionalTexts)
        {
            var newState = _state.With(additionalTexts: _state.AdditionalTexts.AddRange(additionalTexts));
            return FromState(newState);
        }

        //PROTOTYPE: remove arbitrary edit adding and replace with dedicated edit types
        public GeneratorDriver WithPendingEdits(ImmutableArray<PendingEdit> edits)
        {
            var newState = _state.With(edits: _state.Edits.AddRange(edits));
            return FromState(newState);
        }

        private GeneratorDriverState ApplyPartialEdit(GeneratorDriverState state, PendingEdit edit, CancellationToken cancellationToken = default)
        {
            var initialState = state;

            // see if any generators accept this particular edit
            var stateBuilder = PooledDictionary<ISourceGenerator, GeneratorState>.GetInstance();
            foreach (var (generator, generatorState) in state.GeneratorStates)
            {
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
                    state = state.With(generatorStates: state.GeneratorStates.SetItem(generator, generatorState.WithSources(ParseAdditionalSources(context.AdditionalSources.ToImmutableAndFree(), cancellationToken))));
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
            foreach (var (_, generatorState) in state.GeneratorStates)
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

        private ImmutableDictionary<GeneratedSourceText, SyntaxTree> ParseAdditionalSources(ImmutableArray<GeneratedSourceText> generatedSources, CancellationToken cancellationToken)
        {
            var trees = PooledDictionary<GeneratedSourceText, SyntaxTree>.GetInstance();
            foreach (var source in generatedSources)
            {
                trees.Add(source, ParseGeneratedSourceText(source, cancellationToken));
            }
            return trees.ToImmutableDictionaryAndFree();
        }

        private GeneratorDriver BuildFinalCompilation(Compilation compilation, out Compilation outputCompilation, GeneratorDriverState state, CancellationToken cancellationToken)
        {
            ArrayBuilder<SyntaxTree> trees = ArrayBuilder<SyntaxTree>.GetInstance();
            foreach (var (generator, generatorState) in state.GeneratorStates)
            {
                trees.AddRange(generatorState.Sources.Values);
            }
            outputCompilation = compilation.AddSyntaxTrees(trees);
            trees.Free();

            state = state.With(compilation: compilation,
                               finalCompilation: outputCompilation,
                               edits: ImmutableArray<PendingEdit>.Empty,
                               editsFailed: false);
            return FromState(state);
        }

        internal abstract CommonMessageProvider MessageProvider { get; }

        internal abstract GeneratorDriver FromState(GeneratorDriverState state);

        internal abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, CancellationToken cancellationToken);
    }
}
