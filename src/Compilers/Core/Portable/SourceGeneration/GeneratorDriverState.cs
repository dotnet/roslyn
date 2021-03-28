// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct GeneratorDriverState
    {
        internal GeneratorDriverState(ParseOptions parseOptions,
                                      AnalyzerConfigOptionsProvider optionsProvider,
                                      ImmutableArray<ISourceGenerator> generators,
                                      ImmutableArray<AdditionalText> additionalTexts,
                                      ImmutableArray<GeneratorState> generatorStates)
        {
            Generators = generators;
            GeneratorStates = generatorStates;
            AdditionalTexts = additionalTexts;
            ParseOptions = parseOptions;
            OptionsProvider = optionsProvider;

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
        /// ParseOptions to use when parsing generator provided source.
        /// </summary>
        internal readonly ParseOptions ParseOptions;

        internal GeneratorDriverState With(
            ImmutableArray<ISourceGenerator>? generators = null,
            ImmutableArray<GeneratorState>? generatorStates = null,
            ImmutableArray<AdditionalText>? additionalTexts = null,
            bool? editsFailed = null)
        {
            return new GeneratorDriverState(
                this.ParseOptions,
                this.OptionsProvider,
                generators ?? this.Generators,
                additionalTexts ?? this.AdditionalTexts,
                generatorStates ?? this.GeneratorStates
                );
        }
    }
}
