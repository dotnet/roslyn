// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal readonly struct GeneratorDriverState
    {
        internal GeneratorDriverState(ParseOptions parseOptions,
                                      ImmutableArray<ISourceGenerator> generators,
                                      ImmutableArray<AdditionalText> additionalTexts,
                                      ImmutableDictionary<ISourceGenerator, GeneratorState> generatorStates,
                                      ImmutableArray<PendingEdit> edits,
                                      bool editsFailed)
        {
            Generators = generators;
            GeneratorStates = generatorStates;
            AdditionalTexts = additionalTexts;
            Edits = edits;
            ParseOptions = parseOptions;
            EditsFailed = editsFailed;
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
        /// If the driver this state belongs to has yet to perform generation, this will be empty.
        /// After generation there *should* be a 1-to-1 mapping for each generator, unless that generator failed to initialize.
        /// </remarks>
        internal readonly ImmutableDictionary<ISourceGenerator, GeneratorState> GeneratorStates;

        /// <summary>
        /// The set of <see cref="AdditionalText"/>s available to source generators during a run
        /// </summary>
        internal readonly ImmutableArray<AdditionalText> AdditionalTexts;

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
            ParseOptions? parseOptions = null,
            ImmutableArray<ISourceGenerator>? generators = null,
            ImmutableDictionary<ISourceGenerator, GeneratorState>? generatorStates = null,
            ImmutableArray<AdditionalText>? additionalTexts = null,
            ImmutableArray<PendingEdit>? edits = null,
            bool? editsFailed = null)
        {
            return new GeneratorDriverState(
                parseOptions ?? this.ParseOptions,
                generators ?? this.Generators,
                additionalTexts ?? this.AdditionalTexts,
                generatorStates ?? this.GeneratorStates,
                edits ?? this.Edits,
                editsFailed ?? this.EditsFailed
                );
        }
    }
}
