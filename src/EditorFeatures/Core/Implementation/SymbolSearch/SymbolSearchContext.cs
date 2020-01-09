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
        public override CancellationToken CancellationToken { get; }

        internal SymbolSearchSource SymbolSource { get; }

        /// <summary>
        /// <see cref="RoslynSymbolSearchResult"/>s using this <see cref="LocalOrigin"/> 
        /// will be considered exact results, and will be sorted above other results, e.g. remote or from metadata
        /// </summary>
        internal SymbolOrigin LocalOrigin { get; }

        private readonly ISymbolSearchCallback _callback;

        /// <summary>
        /// Stores a mapping from <see cref="DefinitionItem"/> to its matching <see cref="RoslynSymbolSearchResult"/>,
        /// so that we can group <see cref="RoslynSymbolSearchResult"/>s by definition
        /// </summary>
        private readonly Dictionary<DefinitionItem, RoslynSymbolSearchResult> _definitionResults = new Dictionary<DefinitionItem, RoslynSymbolSearchResult>();

        /// <summary>
        /// Protects access to <see cref="_definitionResults"/>
        /// </summary>
        private object _definitionResultsLock = new object();

        public SymbolSearchContext(SymbolSearchSource symbolSource, ISymbolSearchCallback callback, string rootNodeName, CancellationToken cancellationToken)
        {
            this.SymbolSource = symbolSource;
            this.CancellationToken = cancellationToken;
            this.LocalOrigin = new SymbolOrigin(PredefinedSymbolOriginIds.LocalCode, rootNodeName, string.Empty, default);
            _callback = callback;
        }

        public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            if (definition.SourceSpans.Length == 1)
            {
                var result = await RoslynSymbolSearchResult.MakeAsync(this, definition, definition.SourceSpans[0], CancellationToken)
                    .ConfigureAwait(false);
                _callback.AddRange(ImmutableArray.Create<SymbolSearchResult>(result));

                lock (_definitionResultsLock)
                {
                    _definitionResults.Add(definition, result);
                }
            }
            else if (definition.SourceSpans.Length > 1)
            {
                // Create a definition for each of the parts
                var builder = ImmutableArray.CreateBuilder<SymbolSearchResult>(definition.SourceSpans.Length);
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    var result = await RoslynSymbolSearchResult.MakeAsync(this, definition, sourceSpan, CancellationToken)
                        .ConfigureAwait(false);
                    builder.Add(result);
                }
                _callback.AddRange(builder.ToImmutable());
            }

            return;
        }

        public override async Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var result = await RoslynSymbolSearchResult.MakeAsync(this, reference, reference.SourceSpan, CancellationToken)
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
