// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

            var symbolResultsTracker = new DeferredFlushResultSetTracker();

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                // We generate the document contents into an in-memory copy, and then write that out at once at the end. This
                // allows us to collect everything and avoid a lot of fine-grained contention on the write to the single
                // LSIF file. Becasue of the rule that vertices must be written before they're used by an edge, we'll flush any top-
                // level symbol result sets made first, since the document contents will point to that.
                var documentWriter = new InMemoryLsifJsonWriter();
                var documentId = await GenerateForDocument(semanticModel, languageServices, symbolResultsTracker, documentWriter);
                symbolResultsTracker.Flush(_lsifJsonWriter);
                documentWriter.CopyTo(_lsifJsonWriter);

                documentIds.Add(documentId);
            }

            _lsifJsonWriter.Write(Edge.Create("contains", projectVertex.GetId(), documentIds));

            _lsifJsonWriter.Write(new Event(Event.EventKind.End, projectVertex.GetId()));
        }

        private static Task<Id<LsifGraph.Document>> GenerateForDocument(
            SemanticModel semanticModel,
            HostLanguageServices languageServices,
            IResultSetTracker symbolResultsTracker,
            ILsifJsonWriter lsifJsonWriter)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var sourceText = semanticModel.SyntaxTree.GetText();
            var syntaxFactsService = languageServices.GetRequiredService<ISyntaxFactsService>();

            var documentVertex = new LsifGraph.Document(new Uri(syntaxTree.FilePath), GetLanguageKind(semanticModel.Language));

            lsifJsonWriter.Write(documentVertex);
            lsifJsonWriter.Write(new Event(Event.EventKind.Begin, documentVertex.GetId()));

            // We will walk the file token-by-token, making a range for each one and then attaching information for it
            var rangeVertices = new List<Id<LsifGraph.Range>>();

            foreach (var syntaxToken in syntaxTree.GetRoot().DescendantTokens(descendIntoTrivia: true))
            {
                if (syntaxFactsService.IsBindableToken(syntaxToken))
                {
                    var bindableParent = syntaxFactsService.GetBindableParent(syntaxToken);

                    if (bindableParent != null)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(bindableParent);

                        if (symbolInfo.Symbol != null)
                        {
                            var rangeVertex = LsifGraph.Range.FromTextSpan(syntaxToken.Span, sourceText);

                            lsifJsonWriter.Write(rangeVertex);
                            rangeVertices.Add(rangeVertex.GetId());

                            // For now, we will link the range to the original definition. We'll have to fix this once we start supporting
                            // hover, since we show different contents for different constructed types there.
                            var findReferencesSymbol = symbolInfo.Symbol.OriginalDefinition;
                            lsifJsonWriter.Write(Edge.Create("next", rangeVertex.GetId(), symbolResultsTracker.GetResultSetIdForSymbol(findReferencesSymbol)));

                            // Create the link from the references back to this range
                            var referenceResultsId = symbolResultsTracker.GetResultIdForSymbol(findReferencesSymbol, Methods.TextDocumentReferencesName, () => new ReferenceResult());
                            lsifJsonWriter.Write(new Item(referenceResultsId.As<ReferenceResult, Vertex>(), rangeVertex.GetId(), documentVertex.GetId(), property: "references"));
                        }
                    }
                }
            }

            lsifJsonWriter.Write(Edge.Create("contains", documentVertex.GetId(), rangeVertices));
            lsifJsonWriter.Write(new Event(Event.EventKind.End, documentVertex.GetId()));

            return Task.FromResult(documentVertex.GetId());
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
