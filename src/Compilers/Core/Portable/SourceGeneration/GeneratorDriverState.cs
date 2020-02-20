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
    public readonly struct GeneratorDriverState
    {
        internal GeneratorDriverState(Compilation compilation, ParseOptions parseOptions)
            : this(compilation, parseOptions, ImmutableArray<GeneratorProvider>.Empty, ImmutableArray<AdditionalText>.Empty, ImmutableArray<PendingEdit>.Empty, ImmutableDictionary<GeneratorProvider, ImmutableArray<GeneratedSourceText>>.Empty, null, false)
        {
        }

        internal GeneratorDriverState(Compilation compilation, ParseOptions parseOptions, ImmutableArray<GeneratorProvider> providers, ImmutableArray<AdditionalText> additionalTexts, ImmutableArray<PendingEdit> edits, ImmutableDictionary<GeneratorProvider, ImmutableArray<GeneratedSourceText>> sources, Compilation? finalCompilation, bool editsFailed)
        {
            Providers = providers;
            AdditionalTexts = additionalTexts;
            Sources = sources;
            Edits = edits;
            Compilation = compilation;
            ParseOptions = parseOptions;
            FinalCompilation = finalCompilation;
            EditsFailed = editsFailed;
        }

        /// <summary>
        /// The set of <see cref="GeneratorProvider"/>s that will be run
        /// </summary>
        internal readonly ImmutableArray<GeneratorProvider> Providers;

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
        internal readonly ImmutableDictionary<GeneratorProvider, ImmutableArray<GeneratedSourceText>> Sources;

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
            ImmutableArray<GeneratorProvider>? providers = null,
            ImmutableArray<AdditionalText>? additionalTexts = null,
            ImmutableDictionary<GeneratorProvider, ImmutableArray<GeneratedSourceText>>? sources = null,
            ImmutableArray<PendingEdit>? edits = null,
            Compilation? finalCompilation = null,
            bool? editsFailed = null)
        {
            return new GeneratorDriverState(
                compilation ?? this.Compilation,
                parseOptions ?? this.ParseOptions,
                providers ?? this.Providers,
                additionalTexts ?? this.AdditionalTexts,
                edits ?? this.Edits,
                sources ?? this.Sources,
                finalCompilation, // always clear the finalCompilation unless one is explicitly provided
                editsFailed ?? this.EditsFailed
                );
        }
    }
}
