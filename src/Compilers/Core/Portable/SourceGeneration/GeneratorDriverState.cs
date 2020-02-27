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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
    internal readonly struct GeneratorDriverState
    {
        internal GeneratorDriverState(Compilation compilation, ParseOptions parseOptions)
            : this(compilation, parseOptions, ImmutableArray<ISourceGenerator>.Empty, ImmutableArray<AdditionalText>.Empty, ImmutableArray<PendingEdit>.Empty, ImmutableDictionary<ISourceGenerator, ImmutableArray<GeneratedSourceText>>.Empty, null, false)
        {
        }

        internal GeneratorDriverState(Compilation compilation,
                                      ParseOptions parseOptions,
                                      ImmutableArray<ISourceGenerator> generators,
                                      ImmutableArray<AdditionalText> additionalTexts,
                                      ImmutableArray<PendingEdit> edits,
                                      ImmutableDictionary<ISourceGenerator, ImmutableArray<GeneratedSourceText>> sources,
                                      Compilation? finalCompilation,
                                      bool editsFailed)
        {
            Generators = generators;
            AdditionalTexts = additionalTexts;
            Sources = sources;
            Edits = edits;
            Compilation = compilation;
            ParseOptions = parseOptions;
            FinalCompilation = finalCompilation;
            EditsFailed = editsFailed;
        }

        /// <summary>
        /// The set of <see cref="ISourceGenerator"/>s associated with this state.
        /// </summary>
        /// <remarks>
        /// This is the set of generators that will run on next generation.
        /// If there are any sources present in <see cref="Sources" />, they were produced by a subset of these generators.
        /// </remarks>
        internal readonly ImmutableArray<ISourceGenerator> Generators;

        /// <summary>
        /// The set of <see cref="AdditionalText"/>s available to source generators during a run
        /// </summary>
        internal readonly ImmutableArray<AdditionalText> AdditionalTexts;

        /// <summary>
        /// An ordered list of <see cref="PendingEdit"/>s that are waiting to be applied to the compilation.
        /// </summary>
        internal readonly ImmutableArray<PendingEdit> Edits;

        /// <summary>
        /// The set of sources added to this state, by provider that added them
        /// </summary>
        /// <remarks>
        /// If this state has not been used to perform generation, this collection will be empty.
        /// The keys here will be a subset of the <see cref="Generators"/> collection; only genertors
        /// that produced at least 1 source will have an entry.
        /// </remarks>
        internal readonly ImmutableDictionary<ISourceGenerator, ImmutableArray<GeneratedSourceText>> Sources;

        /// <summary>
        /// When set, this contains the <see cref="Compilation"/> with the generated sources applied
        /// </summary>
        internal readonly Compilation? FinalCompilation;

        /// <summary>
        /// Tracks if previous edits have failed to apply. A generator driver will not try and apply any edits when this flag is set.
        /// </summary>
        internal readonly bool EditsFailed;

        /// <summary>
        /// The compilation state before generation
        /// </summary>
        internal readonly Compilation Compilation;

        /// <summary>
        /// ParseOptions to use when parsing generator provided source.
        /// </summary>
        internal readonly ParseOptions ParseOptions;

        internal GeneratorDriverState With(
            Compilation? compilation = null,
            ParseOptions? parseOptions = null,
            ImmutableArray<ISourceGenerator>? generators = null,
            ImmutableArray<AdditionalText>? additionalTexts = null,
            ImmutableDictionary<ISourceGenerator, ImmutableArray<GeneratedSourceText>>? sources = null,
            ImmutableArray<PendingEdit>? edits = null,
            Compilation? finalCompilation = null,
            bool? editsFailed = null)
        {
            return new GeneratorDriverState(
                compilation ?? this.Compilation,
                parseOptions ?? this.ParseOptions,
                generators ?? this.Generators,
                additionalTexts ?? this.AdditionalTexts,
                edits ?? this.Edits,
                sources ?? this.Sources,
                finalCompilation, // always clear the finalCompilation unless one is explicitly provided
                editsFailed ?? this.EditsFailed
                );
        }
    }
}
