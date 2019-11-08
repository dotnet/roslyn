// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class SymbolSearchContext : FindUsagesContext
    {
        internal RoslynSymbolSource SymbolSource { get; }
        internal LocalCodeSymbolOrigin RootSymbolOrigin { get; }

        private readonly IStreamingSymbolSearchSink SymbolSink;
        public new readonly CancellationToken CancellationToken;
        private readonly Dictionary<DefinitionItem, RoslynSymbolResult> DefinitionResults = new Dictionary<DefinitionItem, RoslynSymbolResult>();
        private object Gate = new object();

        public SymbolSearchContext(RoslynSymbolSource symbolSource, IStreamingSymbolSearchSink sink, string rootNodeName)
        {
            this.SymbolSource = symbolSource;
            this.SymbolSink = sink;
            this.CancellationToken = sink.CancellationToken;
            this.RootSymbolOrigin = new LocalCodeSymbolOrigin(rootNodeName);
        }

        public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            if (definition.SourceSpans.Length == 1)
            {
                var result = await RoslynSymbolResult.MakeAsync(this, definition, definition.SourceSpans[0], CancellationToken)
                    .ConfigureAwait(false);
                this.SymbolSink.Add(result);
                lock (Gate)
                {
                    this.DefinitionResults.Add(definition, result);
                }
            }
            else if (definition.SourceSpans.Length > 1)
            {
                // Create a definition for each of the parts
                var builder = ImmutableArray.CreateBuilder<SymbolSearchResult>(definition.SourceSpans.Length);
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    var result = await RoslynSymbolResult.MakeAsync(this, definition, sourceSpan, CancellationToken)
                        .ConfigureAwait(false);
                    builder.Add(result);
                }
                this.SymbolSink.AddRange(builder.ToImmutable());
            }

            return;
        }

        public override async Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var result = await RoslynSymbolResult.MakeAsync(this, reference, reference.SourceSpan, CancellationToken)
                .ConfigureAwait(false);
            this.SymbolSink.Add(result);
            return;
        }

        internal RoslynSymbolResult GetDefinitionResult(DefinitionItem definition)
        {
            lock (Gate)
            {
                if (DefinitionResults.TryGetValue(definition, out var definitionResult))
                {
                    return definitionResult;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
