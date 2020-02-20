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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public abstract class GeneratorDriver
    {
        protected readonly GeneratorDriverState _state;

        protected GeneratorDriver(GeneratorDriverState state)
        {
            _state = state;
        }

        protected GeneratorDriver(Compilation compilation, ParseOptions parseOptions)
        {
            _state = new GeneratorDriverState(compilation, parseOptions, ImmutableArray<GeneratorProvider>.Empty, ImmutableArray<AdditionalText>.Empty, ImmutableArray<PendingEdit>.Empty, ImmutableDictionary<GeneratorProvider, ImmutableArray<GeneratedSourceText>>.Empty, finalCompilation: null, editsFailed: true);
        }

        public GeneratorDriver RunFullGeneration(Compilation compilation, out Compilation outputCompilation, CancellationToken cancellationToken = default)
        {
            // with no providers, there is no work to do
            if (_state.Providers.Length == 0)
            {
                outputCompilation = compilation;
                return this;
            }

            // PERF: if the input compilation is the same as the last compilation we saw, and we have a final compilation
            //       we know nothing can have changed and can just short circuit, returning the already processed final compilation 
            if (compilation == _state.Compilation && _state.FinalCompilation is object)
            {
                outputCompilation = _state.FinalCompilation;
                return this;
            }

            // run the actual generation
            var state = StateWithPendingEditsApplied(_state);
            var sourcesBuilder = PooledDictionary<GeneratorProvider, ImmutableArray<GeneratedSourceText>>.GetInstance();

            //PROTOTYPE: should be possible to parallelize this
            foreach (var provider in state.Providers)
            {
                try
                {
                    // we create a new context for each run of the generator. We'll never re-use existing state, only replace anything we have
                    var context = new SourceGeneratorContext(state.Compilation, new AnalyzerOptions(state.AdditionalTexts.NullToEmpty(), CompilerAnalyzerConfigOptionsProvider.Empty));

                    // PROTOTYPE: we call provider.GetGenerator(). Should we cache it here, or rely on the provider to do so?
                    provider.GetGenerator().Execute(context);
                    sourcesBuilder.Add(provider, context.AdditionalSources.ToImmutableAndFree());
                }
                catch
                {
                    //PROTOTYPE: we should issue a diagnostic that the generator failed
                }
            }
            state = state.With(sources: sourcesBuilder.ToImmutableDictionaryAndFree());

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

            success = true;
            return BuildFinalCompilation(compilation, out outputCompilation, state, cancellationToken);
        }

        public GeneratorDriver WithGeneratorProviders(ImmutableArray<GeneratorProvider> providers)
        {
            var newState = _state.With(providers: _state.Providers.AddRange(providers));
            return FromState(newState);
        }

        public GeneratorDriver WithAdditionalTexts(ImmutableArray<AdditionalText> additionalTexts)
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

        //PROTOTYPE
        public bool ContainsAdditionalText(AdditionalText text) => _state.AdditionalTexts.Contains(text);

        private static GeneratorDriverState ApplyPartialEdit(GeneratorDriverState state, PendingEdit edit, CancellationToken cancellationToken = default)
        {
            var initialState = state;

            // see if any generators accept this particular edit
            foreach (var provider in state.Providers)
            {
                var generator = provider.GetGenerator();
                if (edit.AcceptedBy(generator))
                {
                    // attempt to apply the edit
                    var context = new UpdateContext(state.Sources[provider], cancellationToken);
                    var succeeded = edit.TryApply(generator, context);
                    if (!succeeded)
                    {
                        // we couldn't successfully apply this edit
                        // return the original state noting we failed
                        return initialState.With(editsFailed: true);
                    }

                    // update the state with the new edits
                    state = state.With(sources: state.Sources.SetItem(provider, context.AdditionalSources.ToImmutableAndFree()));
                }
            }

            return state;
        }

        private static GeneratorDriverState StateWithPendingEditsApplied(GeneratorDriverState state)
        {
            if (state.Edits.Length == 0)
            {
                return state;
            }

            var newState = state;
            foreach (var edit in newState.Edits)
            {
                newState = edit.Commit(newState);
            }
            return newState.With(edits: ImmutableArray<PendingEdit>.Empty, editsFailed: false);
        }

        private GeneratorDriver BuildFinalCompilation(Compilation compilation, out Compilation outputCompilation, GeneratorDriverState state, CancellationToken cancellationToken)
        {
            var finalCompilation = compilation;
            foreach (var (_, sourcesAdded) in state.Sources)
            {
                foreach (var sourceText in sourcesAdded)
                {
                    try
                    {
                        //PROTOTYPE: should be possible to parallelize the parsing
                        var tree = ParseGeneratedSourceText(sourceText, cancellationToken);
                        finalCompilation = finalCompilation.AddSyntaxTrees(tree);
                    }
                    catch
                    {
                        //PROTOTYPE: should issue a diagnostic that the generator produced unparseable source
                    }
                }
            }
            outputCompilation = finalCompilation;
            state = state.With(compilation: compilation,
                               finalCompilation: finalCompilation,
                               edits: ImmutableArray<PendingEdit>.Empty,
                               editsFailed: false);
            return FromState(state);
        }

        protected abstract GeneratorDriver FromState(GeneratorDriverState state);

        protected abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, CancellationToken cancellationToken);
    }
}
