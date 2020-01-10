// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SymbolSearch
{
    internal class SymbolSearchContext : FindUsagesContext
    {
        /// <inheritdoc/>
        public override CancellationToken CancellationToken { get; }

        /// <summary>
        /// Allows <see cref="RoslynSymbolSearchResult"/> to access the <see cref="ISymbolSource"/>
        /// </summary>
        internal readonly SymbolSearchSource SymbolSource;

        /// <summary>
        /// <see cref="RoslynSymbolSearchResult"/>s using this <see cref="SymbolOrigin"/> 
        /// will be considered exact results, and will be sorted above other results, e.g. remote or from metadata
        /// </summary>
        private readonly SymbolOrigin _localOrigin;

        /// <summary>
        /// <see cref="RoslynSymbolSearchResult"/>s using this <see cref="SymbolOrigin"/> 
        /// will be considered metadata results, and will be sorted below remote and exact.
        /// </summary>
        private readonly SymbolOrigin _metadataOrigin;

        /// <summary>
        /// Used to report results back to the Symbol Search engine
        /// </summary>
        private readonly ISymbolSearchCallback _callback;

        /// <summary>
        /// Stores a mapping from <see cref="DefinitionItem"/> to its matching <see cref="RoslynSymbolSearchResult"/>,
        /// so that we can group <see cref="RoslynSymbolSearchResult"/>s by definition
        /// </summary>
        private readonly Dictionary<DefinitionItem, RoslynSymbolSearchResult> _definitionResults = new Dictionary<DefinitionItem, RoslynSymbolSearchResult>();

        /// <summary>
        /// Protects access to <see cref="_definitionResults"/>
        /// </summary>
        private readonly object _definitionResultsLock = new object();

        public SymbolSearchContext(SymbolSearchSource symbolSource, ISymbolSearchCallback callback, string rootNodeName, CancellationToken cancellationToken)
        {
            this.SymbolSource = symbolSource;
            this.CancellationToken = cancellationToken;
            _localOrigin = new SymbolOrigin(PredefinedSymbolOriginIds.LocalCode, rootNodeName, string.Empty, default);
            _metadataOrigin = new SymbolOrigin(PredefinedSymbolOriginIds.Metadata, EditorFeaturesResources.Symbol_search_current_solutions_dependencies, string.Empty, default);
            _callback = callback;
        }

        public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            if (definition.SourceSpans.Length == 1)
            {
                // If we only have a single location, then use the DisplayParts of the
                // definition as what to show.  That way we show enough information for things
                // methods.  i.e. we'll show "void TypeName.MethodName(args...)" allowing
                // the user to see the type the method was created in.
                var result = await RoslynSymbolSearchResult.MakeAsync(this, _localOrigin, definition, definition.SourceSpans[0], CancellationToken)
                    .ConfigureAwait(false);
                _callback.AddRange(ImmutableArray.Create<SymbolSearchResult>(result));

                lock (_definitionResultsLock)
                {
                    _definitionResults.Add(definition, result);
                }
            }
            else if (definition.SourceSpans.Length == 0)
            {
                // No source spans means metadata references.
                // Display it for Go to Base and try to navigate to metadata.
                var result = await RoslynSymbolSearchResult.MakeAsync(this, _metadataOrigin, definition, default, CancellationToken)
                    .ConfigureAwait(false);
                _callback.AddRange(ImmutableArray.Create<SymbolSearchResult>(result));
            }
            else
            {
                // If we have multiple spans (i.e. for partial types), then create a 
                // DocumentSpanEntry for each.  That way we can easily see the source
                // code where each location is to help the user decide which they want
                // to navigate to.
                var builder = ImmutableArray.CreateBuilder<SymbolSearchResult>(definition.SourceSpans.Length);
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    var result = await RoslynSymbolSearchResult.MakeAsync(this, _localOrigin, definition, sourceSpan, CancellationToken)
                        .ConfigureAwait(false);
                    builder.Add(result);
                }
                _callback.AddRange(builder.ToImmutable());
            }

            return;
        }

        public override async Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var result = await RoslynSymbolSearchResult.MakeAsync(this, _localOrigin, reference, reference.SourceSpan, CancellationToken)
                .ConfigureAwait(false);
            _callback.AddRange(ImmutableArray.Create<SymbolSearchResult>(result));
            return;
        }

        internal RoslynSymbolSearchResult GetDefinitionResult(DefinitionItem definition)
        {
            lock (_definitionResultsLock)
            {
                return _definitionResults.TryGetValue(definition, out var definitionResult)
                    ? definitionResult : null;
            }
        }
    }
}
