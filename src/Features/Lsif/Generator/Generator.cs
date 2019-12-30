// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;
using Microsoft.CodeAnalysis.Lsif.Generator.ResultSetTracking;
using Microsoft.CodeAnalysis.Lsif.Generator.Writing;
using Methods = Microsoft.VisualStudio.LanguageServer.Protocol.Methods;

namespace Microsoft.CodeAnalysis.Lsif.Generator
{
    internal sealed class Generator
    {
        private readonly ILsifJsonWriter _lsifJsonWriter;

        public Generator(ILsifJsonWriter lsifJsonWriter)
        {
            _lsifJsonWriter = lsifJsonWriter;
        }

        public async Task GenerateForCompilation(Compilation compilation, string projectPath, HostLanguageServices languageServices)
        {
            var projectVertex = new LsifGraph.Project(kind: GetLanguageKind(compilation.Language), new Uri(projectPath));
            _lsifJsonWriter.Write(projectVertex);
            _lsifJsonWriter.Write(new Event(Event.EventKind.Begin, projectVertex.GetId()));

            var documentIds = new List<Id<LsifGraph.Document>>();

            // We create a ResultSetTracker to track all top-level symbols in the project. We don't want all writes to immediately go to
            // the JSON file once we support parallel processing, so we'll accumulate them and then apply at once.
            var topLevelSymbolsWriter = new InMemoryLsifJsonWriter();
            var topLevelSymbolsResultSetTracker = new SymbolHoldingResultSetTracker(topLevelSymbolsWriter, compilation);

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                // We generate the document contents into an in-memory copy, and then write that out at once at the end. This
                // allows us to collect everything and avoid a lot of fine-grained contention on the write to the single
                // LSIF file. Becasue of the rule that vertices must be written before they're used by an edge, we'll flush any top-
                // level symbol result sets made first, since the document contents will point to that.
                var documentWriter = new InMemoryLsifJsonWriter();
                var documentId = await GenerateForDocument(semanticModel, languageServices, topLevelSymbolsResultSetTracker, documentWriter);
                topLevelSymbolsWriter.CopyToAndEmpty(_lsifJsonWriter);
                documentWriter.CopyToAndEmpty(_lsifJsonWriter);

                documentIds.Add(documentId);
            }

            _lsifJsonWriter.Write(Edge.Create("contains", projectVertex.GetId(), documentIds));

            _lsifJsonWriter.Write(new Event(Event.EventKind.End, projectVertex.GetId()));
        }

        private static Task<Id<LsifGraph.Document>> GenerateForDocument(
            SemanticModel semanticModel,
            HostLanguageServices languageServices,
            IResultSetTracker topLevelSymbolsResultSetTracker,
            ILsifJsonWriter lsifJsonWriter)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var sourceText = semanticModel.SyntaxTree.GetText();
            var syntaxFactsService = languageServices.GetRequiredService<ISyntaxFactsService>();
            var semanticFactsService = languageServices.GetRequiredService<ISemanticFactsService>();

            var documentVertex = new LsifGraph.Document(new Uri(syntaxTree.FilePath), GetLanguageKind(semanticModel.Language));

            lsifJsonWriter.Write(documentVertex);
            lsifJsonWriter.Write(new Event(Event.EventKind.Begin, documentVertex.GetId()));

            // As we are processing this file, we are going to encounter symbols that have a shared resultSet with other documents like types
            // or methods. We're also going to encounter locals that never leave this document. We don't want those locals being held by
            // the topLevelSymbolsResultSetTracker, so we'll make another tracker for document local symbols, and then have a delegating
            // one that picks the correct one of the two.
            var documentLocalSymbolsResultSetTracker = new SymbolHoldingResultSetTracker(lsifJsonWriter, semanticModel.Compilation);
            var symbolResultsTracker = new DelegatingResultSetTracker(symbol =>
            {
                if (symbol.Kind == SymbolKind.Local ||
                    symbol.Kind == SymbolKind.RangeVariable ||
                    symbol.Kind == SymbolKind.Label)
                {
                    // These symbols can go in the document local one because they can't escape methods
                    return documentLocalSymbolsResultSetTracker;
                }
                else if (symbol.ContainingType != null && symbol.DeclaredAccessibility == Accessibility.Private && symbol.ContainingType.Locations.Length == 1)
                {
                    // This is a private member in a class that isn't partial, so it can't escape the file
                    return documentLocalSymbolsResultSetTracker;
                }
                else
                {
                    return topLevelSymbolsResultSetTracker;
                }
            });

            // We will walk the file token-by-token, making a range for each one and then attaching information for it
            var rangeVertices = new List<Id<LsifGraph.Range>>();

            foreach (var syntaxToken in syntaxTree.GetRoot().DescendantTokens(descendIntoTrivia: true))
            {
                // We'll only create the Range vertex once it's needed, but any number of bits of code might create it first,
                // so we'll just make it Lazy.
                var lazyRangeVertex = new Lazy<LsifGraph.Range>(() =>
                {
                    var rangeVertex = LsifGraph.Range.FromTextSpan(syntaxToken.Span, sourceText);

                    lsifJsonWriter.Write(rangeVertex);
                    rangeVertices.Add(rangeVertex.GetId());

                    return rangeVertex;
                }, LazyThreadSafetyMode.None);

                var declaredSymbol = semanticFactsService.GetDeclaredSymbol(semanticModel, syntaxToken, CancellationToken.None);
                ISymbol? referencedSymbol = null;

                if (syntaxFactsService.IsBindableToken(syntaxToken))
                {
                    var bindableParent = syntaxFactsService.GetBindableParent(syntaxToken);

                    if (bindableParent != null)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(bindableParent);
                        if (symbolInfo.Symbol != null && IncludeSymbolInReferences(symbolInfo.Symbol))
                        {
                            referencedSymbol = symbolInfo.Symbol;
                        }
                    }
                }

                if (declaredSymbol != null || referencedSymbol != null)
                {
                    // For now, we will link the range to the original definition, preferring the definition, as this is the symbol
                    // that would be used if we invoke a feature on this range. This is analogous to the logic in
                    // SymbolFinder.FindSymbolAtPositionAsync where if a token is both a reference and definition we'll prefer the
                    // definition. Once we start supporting hover we'll hae to remove the "original defintion" part of this, since
                    // since we show different contents for different constructed types there.
                    var symbolForLinkedResultSet = (declaredSymbol ?? referencedSymbol)!.OriginalDefinition;
                    var symbolForLinkedResultSetId = symbolResultsTracker.GetResultSetIdForSymbol(symbolForLinkedResultSet);
                    lsifJsonWriter.Write(Edge.Create("next", lazyRangeVertex.Value.GetId(), symbolForLinkedResultSetId));

                    if (declaredSymbol != null)
                    {
                        var definitionResultsId = symbolResultsTracker.GetResultIdForSymbol(declaredSymbol, Methods.TextDocumentDefinitionName, () => new DefinitionResult());
                        lsifJsonWriter.Write(new Item(definitionResultsId.As<DefinitionResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId()));
                    }

                    if (referencedSymbol != null)
                    {
                        // Create the link from the references back to this range. Note: this range can be reference to a
                        // symbol but the range can point a different symbol's resultSet. This can happen if the token is
                        // both a definition of a symbol (where we will point to the definition) but also a reference to some
                        // other symbol.
                        var referenceResultsId = symbolResultsTracker.GetResultIdForSymbol(referencedSymbol.OriginalDefinition, Methods.TextDocumentReferencesName, () => new ReferenceResult());
                        lsifJsonWriter.Write(new Item(referenceResultsId.As<ReferenceResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId(), property: "references"));
                    }
                }
            }

            lsifJsonWriter.Write(Edge.Create("contains", documentVertex.GetId(), rangeVertices));
            lsifJsonWriter.Write(new Event(Event.EventKind.End, documentVertex.GetId()));

            return Task.FromResult(documentVertex.GetId());
        }

        private static bool IncludeSymbolInReferences(ISymbol symbol)
        {
            // Skip some type of symbols that don't really make sense
            if (symbol.Kind == SymbolKind.ArrayType ||
                symbol.Kind == SymbolKind.Discard ||
                symbol.Kind == SymbolKind.ErrorType)
            {
                return false;
            }

            return true;
        }

        private static string GetLanguageKind(string languageName)
        {
            return languageName switch
            {
                LanguageNames.CSharp => "csharp",
                LanguageNames.VisualBasic => "vb",
                _ => throw new NotSupportedException(languageName),
            };
        }
    }
}
