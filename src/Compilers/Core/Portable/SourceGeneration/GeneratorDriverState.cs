// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal readonly struct GeneratorDriverState
    {
        internal GeneratorDriverState(ParseOptions parseOptions,
                                      AnalyzerConfigOptionsProvider optionsProvider,
                                      ImmutableArray<ISourceGenerator> generators,
                                      ImmutableArray<AdditionalText> additionalTexts,
                                      ImmutableArray<GeneratorState> generatorStates,
                                      ImmutableArray<PendingEdit> edits,
                                      bool editsFailed)
        {
            Generators = generators;
            GeneratorStates = generatorStates;
            AdditionalTexts = additionalTexts;
            Edits = edits;
            ParseOptions = parseOptions;
            OptionsProvider = optionsProvider;
            EditsFailed = editsFailed;

            Debug.Assert(Generators.Length == GeneratorStates.Length);
        }

        /// <summary>
        /// The set of <see cref="ISourceGenerator"/>s associated with this state.
        /// </summary>
        /// <remarks>
        /// This is the set of generators that will run on next generation.
        /// If there are any states present in <see cref="GeneratorStates" />, they were produced by a subset of these generators.
        /// </remarks>
        internal readonly ImmutableArray<ISourceGenerator> Generators;

        /// <summary>
        /// The last run state of each generator, by the generator that created it
        /// </summary>
        /// <remarks>
        /// There will be a 1-to-1 mapping for each generator. If a generator has yet to
        /// be initialized or failed during initialization it's state will be <c>default(GeneratorState)</c>
        /// </remarks>
        internal readonly ImmutableArray<GeneratorState> GeneratorStates;

        /// <summary>
        /// The set of <see cref="AdditionalText"/>s available to source generators during a run
        /// </summary>
        internal readonly ImmutableArray<AdditionalText> AdditionalTexts;

        /// <summary>
        /// Gets a provider for analyzer options
        /// </summary>
        internal readonly AnalyzerConfigOptionsProvider OptionsProvider;

        /// <summary>
        /// An ordered list of <see cref="PendingEdit"/>s that are waiting to be applied to the compilation.
        /// </summary>
        internal readonly ImmutableArray<PendingEdit> Edits;

        /// <summary>
        /// Tracks if previous edits have failed to apply. A generator driver will not try and apply any edits when this flag is set.
        /// </summary>
        internal readonly bool EditsFailed;

        /// <summary>
        /// ParseOptions to use when parsing generator provided source.
        /// </summary>
        internal readonly ParseOptions ParseOptions;

        internal GeneratorDriverState With(
            ImmutableArray<ISourceGenerator>? generators = null,
            ImmutableArray<GeneratorState>? generatorStates = null,
            ImmutableArray<AdditionalText>? additionalTexts = null,
            ImmutableArray<PendingEdit>? edits = null,
            bool? editsFailed = null)
        {
            return new GeneratorDriverState(
                this.ParseOptions,
                this.OptionsProvider,
                generators ?? this.Generators,
                additionalTexts ?? this.AdditionalTexts,
                generatorStates ?? this.GeneratorStates,
                edits ?? this.Edits,
                editsFailed ?? this.EditsFailed
                );
        }
    }
}
