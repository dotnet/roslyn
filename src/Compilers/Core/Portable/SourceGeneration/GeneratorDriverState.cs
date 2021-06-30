﻿// Licensed to the .NET Foundation under one or more agreements.
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
                                      ImmutableArray<ISourceGenerator> sourceGenerators,
                                      ImmutableArray<IIncrementalGenerator> incrementalGenerators,
                                      ImmutableArray<AdditionalText> additionalTexts,
                                      ImmutableArray<GeneratorState> generatorStates,
                                      DriverStateTable stateTable,
                                      bool enableIncremental)
        {
            Generators = sourceGenerators;
            IncrementalGenerators = incrementalGenerators;
            GeneratorStates = generatorStates;
            AdditionalTexts = additionalTexts;
            ParseOptions = parseOptions;
            OptionsProvider = optionsProvider;
            StateTable = stateTable;
            EnableIncremental = enableIncremental;
            Debug.Assert(Generators.Length == GeneratorStates.Length);
            Debug.Assert(IncrementalGenerators.Length == GeneratorStates.Length);
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
        /// The set of <see cref="IIncrementalGenerator"/>s associated with this state.
        /// </summary>
        /// <remarks>
        /// This is the 'internal' representation of the <see cref="Generators"/> collection. There is a 1-to-1 mapping
        /// where each entry is either the unwrapped incremental generator or a wrapped <see cref="ISourceGenerator"/>
        /// </remarks>
        internal readonly ImmutableArray<IIncrementalGenerator> IncrementalGenerators;

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

        internal readonly DriverStateTable StateTable;

        /// <summary>
        /// Should this driver run incremental generators or not
        /// </summary>
        /// <remarks>
        /// Only used during preview period when incremental generators are enabled/disabled by preview flag
        /// </remarks>
        internal readonly bool EnableIncremental;

        internal GeneratorDriverState With(
            ImmutableArray<ISourceGenerator>? sourceGenerators = null,
            ImmutableArray<IIncrementalGenerator>? incrementalGenerators = null,
            ImmutableArray<GeneratorState>? generatorStates = null,
            ImmutableArray<AdditionalText>? additionalTexts = null,
            DriverStateTable? stateTable = null,
            ParseOptions? parseOptions = null,
            AnalyzerConfigOptionsProvider? optionsProvider = null)
        {
            return new GeneratorDriverState(
                parseOptions ?? this.ParseOptions,
                optionsProvider ?? this.OptionsProvider,
                sourceGenerators ?? this.Generators,
                incrementalGenerators ?? this.IncrementalGenerators,
                additionalTexts ?? this.AdditionalTexts,
                generatorStates ?? this.GeneratorStates,
                stateTable ?? this.StateTable,
                this.EnableIncremental
                );
        }
    }
}
