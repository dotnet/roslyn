// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;
using Methods = Microsoft.VisualStudio.LanguageServer.Protocol.Methods;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator
{
    internal sealed class Generator
    {
        // LSIF generator capabilities. See https://github.com/microsoft/lsif-node/blob/main/protocol/src/protocol.ts#L925 for details.
        private const bool HoverProvider = true;
        private const bool DeclarationProvider = false;
        private const bool DefinitionProvider = true;
        private const bool ReferencesProvider = true;
        private const bool TypeDefinitionProvider = false;
        private const bool DocumentSymbolProvider = false;
        private const bool FoldingRangeProvider = true;
        private const bool DiagnosticProvider = false;

        private readonly ILsifJsonWriter _lsifJsonWriter;
        private readonly IdFactory _idFactory = new IdFactory();

        private Generator(ILsifJsonWriter lsifJsonWriter)
        {
            _lsifJsonWriter = lsifJsonWriter;
        }

        public static Generator CreateAndWriteCapabilitiesVertex(ILsifJsonWriter lsifJsonWriter)
        {
            var generator = new Generator(lsifJsonWriter);
            var capabilitiesVertex = new Capabilities(generator._idFactory,
                HoverProvider, DeclarationProvider, DefinitionProvider, ReferencesProvider,
                TypeDefinitionProvider, DocumentSymbolProvider, FoldingRangeProvider, DiagnosticProvider);
            generator._lsifJsonWriter.Write(capabilitiesVertex);
            return generator;
        }

        public async Task GenerateForCompilationAsync(Compilation compilation, string projectPath, HostLanguageServices languageServices, GeneratorOptions options)
        {
            var projectVertex = new Graph.LsifProject(kind: GetLanguageKind(compilation.Language), new Uri(projectPath), _idFactory);
            _lsifJsonWriter.Write(projectVertex);
            _lsifJsonWriter.Write(new Event(Event.EventKind.Begin, projectVertex.GetId(), _idFactory));

            var documentIds = new ConcurrentBag<Id<Graph.LsifDocument>>();

            // We create a ResultSetTracker to track all top-level symbols in the project. We don't want all writes to immediately go to
            // the JSON file -- we support parallel processing, so we'll accumulate them and then apply at once to avoid a lot
            // of contention on shared locks.
            var topLevelSymbolsWriter = new BatchingLsifJsonWriter(_lsifJsonWriter);
            var topLevelSymbolsResultSetTracker = new SymbolHoldingResultSetTracker(topLevelSymbolsWriter, compilation, _idFactory);

            // Disable navigation hints in quick info as computing them both takes too long, and they're never
            // even emitted in the final lsif hover information.
            options = options with
            {
                SymbolDescriptionOptions = options.SymbolDescriptionOptions with
                {
                    QuickInfoOptions = options.SymbolDescriptionOptions.QuickInfoOptions with
                    {
                        IncludeNavigationHintsInQuickInfo = false
                    }
                }
            };

            var tasks = new List<Task>();
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    // We generate the document contents into an in-memory copy, and then write that out at once at the end. This
                    // allows us to collect everything and avoid a lot of fine-grained contention on the write to the single
                    // LSIF file. Because of the rule that vertices must be written before they're used by an edge, we'll flush any top-
                    // level symbol result sets made first, since the document contents will point to that. Parallel calls to CopyAndEmpty
                    // are allowed and might flush other unrelated stuff at the same time, but there's no harm -- the "causality" ordering
                    // is preserved.
                    var documentWriter = new BatchingLsifJsonWriter(_lsifJsonWriter);
                    var documentId = await GenerateForDocumentAsync(semanticModel, languageServices, options, topLevelSymbolsResultSetTracker, documentWriter, _idFactory);
                    topLevelSymbolsWriter.FlushToUnderlyingAndEmpty();
                    documentWriter.FlushToUnderlyingAndEmpty();

                    documentIds.Add(documentId);
                }));
            }

            await Task.WhenAll(tasks);

            _lsifJsonWriter.Write(Edge.Create("contains", projectVertex.GetId(), documentIds.ToArray(), _idFactory));

            _lsifJsonWriter.Write(new Event(Event.EventKind.End, projectVertex.GetId(), _idFactory));
        }

        /// <summary>
        /// Generates the LSIF content for a single document.
        /// </summary>
        /// <returns>The ID of the outputted Document vertex.</returns>
        /// <remarks>
        /// The high level algorithm here is we are going to walk across each token, produce a <see cref="Graph.Range"/> for that token's span,
        /// bind that token, and then link up the various features. So we'll link that range to the symbols it defines or references,
        /// will link it to results like Quick Info, and more. This method has a <paramref name="topLevelSymbolsResultSetTracker"/> that
        /// lets us link symbols across files, and will only talk about "top level" symbols that aren't things like locals that can't
        /// leak outside a file.
        /// </remarks>
        private static async Task<Id<Graph.LsifDocument>> GenerateForDocumentAsync(
            SemanticModel semanticModel,
            HostLanguageServices languageServices,
            GeneratorOptions options,
            IResultSetTracker topLevelSymbolsResultSetTracker,
            ILsifJsonWriter lsifJsonWriter,
            IdFactory idFactory)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var sourceText = semanticModel.SyntaxTree.GetText();
            var syntaxFactsService = languageServices.GetRequiredService<ISyntaxFactsService>();
            var semanticFactsService = languageServices.GetRequiredService<ISemanticFactsService>();

            string? contentBase64Encoded = null;

            var uri = syntaxTree.FilePath;

            // TODO: move to checking the enum member mentioned in https://github.com/dotnet/roslyn/issues/49326 when that
            // is implemented. In the mean time, we'll use a heuristic of the path being a relative path as a way to indicate
            // this is a source generated file.
            if (!PathUtilities.IsAbsolute(syntaxTree.FilePath))
            {
                var text = semanticModel.SyntaxTree.GetText();

                // We always use UTF-8 encoding when writing out file contents, as that's expected by LSIF implementations.
                // TODO: when we move to .NET Core, is there a way to reduce allocations here?
                contentBase64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(text.ToString()));

                // There is a triple slash here, so the "host" portion of the URI is empty, similar to
                // how file URIs work.
                uri = "source-generated:///" + syntaxTree.FilePath.Replace('\\', '/');
            }

            var documentVertex = new Graph.LsifDocument(new Uri(uri, UriKind.RelativeOrAbsolute), GetLanguageKind(semanticModel.Language), contentBase64Encoded, idFactory);

            lsifJsonWriter.Write(documentVertex);
            lsifJsonWriter.Write(new Event(Event.EventKind.Begin, documentVertex.GetId(), idFactory));

            // As we are processing this file, we are going to encounter symbols that have a shared resultSet with other documents like types
            // or methods. We're also going to encounter locals that never leave this document. We don't want those locals being held by
            // the topLevelSymbolsResultSetTracker, so we'll make another tracker for document local symbols, and then have a delegating
            // one that picks the correct one of the two.
            var documentLocalSymbolsResultSetTracker = new SymbolHoldingResultSetTracker(lsifJsonWriter, semanticModel.Compilation, idFactory);
            var symbolResultsTracker = new DelegatingResultSetTracker(symbol =>
            {
                if (symbol.Kind is SymbolKind.Local or
                    SymbolKind.RangeVariable or
                    SymbolKind.Label)
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
            var rangeVertices = new List<Id<Graph.Range>>();

            foreach (var syntaxToken in syntaxTree.GetRoot().DescendantTokens(descendIntoTrivia: true))
            {
                // We'll only create the Range vertex once it's needed, but any number of bits of code might create it first,
                // so we'll just make it Lazy.
                var lazyRangeVertex = new Lazy<Graph.Range>(() =>
                {
                    var rangeVertex = Graph.Range.FromTextSpan(syntaxToken.Span, sourceText, idFactory);

                    lsifJsonWriter.Write(rangeVertex);
                    rangeVertices.Add(rangeVertex.GetId());

                    return rangeVertex;
                }, LazyThreadSafetyMode.None);

                var declaredSymbol = semanticFactsService.GetDeclaredSymbol(semanticModel, syntaxToken, CancellationToken.None);
                ISymbol? referencedSymbol = null;

                if (syntaxFactsService.IsBindableToken(syntaxToken))
                {
                    var bindableParent = syntaxFactsService.TryGetBindableParent(syntaxToken);

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
                    // definition. Once we start supporting hover we'll have to remove the "original definition" part of this, since
                    // since we show different contents for different constructed types there.
                    var symbolForLinkedResultSet = (declaredSymbol ?? referencedSymbol)!.OriginalDefinition;
                    var symbolForLinkedResultSetId = symbolResultsTracker.GetResultSetIdForSymbol(symbolForLinkedResultSet);
                    lsifJsonWriter.Write(Edge.Create("next", lazyRangeVertex.Value.GetId(), symbolForLinkedResultSetId, idFactory));

                    if (declaredSymbol != null)
                    {
                        var definitionResultsId = symbolResultsTracker.GetResultIdForSymbol(declaredSymbol, Methods.TextDocumentDefinitionName, () => new DefinitionResult(idFactory));
                        lsifJsonWriter.Write(new Item(definitionResultsId.As<DefinitionResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId(), idFactory));
                    }

                    if (referencedSymbol != null)
                    {
                        // Create the link from the references back to this range. Note: this range can be reference to a
                        // symbol but the range can point a different symbol's resultSet. This can happen if the token is
                        // both a definition of a symbol (where we will point to the definition) but also a reference to some
                        // other symbol.
                        var referenceResultsId = symbolResultsTracker.GetResultIdForSymbol(referencedSymbol.OriginalDefinition, Methods.TextDocumentReferencesName, () => new ReferenceResult(idFactory));
                        lsifJsonWriter.Write(new Item(referenceResultsId.As<ReferenceResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId(), idFactory, property: "references"));
                    }

                    // Write hover information for the symbol, if edge has not already been added.
                    // 'textDocument/hover' edge goes from the symbol ResultSet vertex to the hover result
                    // See https://github.com/Microsoft/language-server-protocol/blob/main/indexFormat/specification.md#resultset for an example.
                    if (symbolResultsTracker.ResultSetNeedsInformationalEdgeAdded(symbolForLinkedResultSet, Methods.TextDocumentHoverName))
                    {
                        var hover = await HoverHandler.GetHoverAsync(semanticModel, syntaxToken.SpanStart, options.SymbolDescriptionOptions, languageServices, CancellationToken.None);
                        if (hover != null)
                        {
                            var hoverResult = new HoverResult(hover, idFactory);
                            lsifJsonWriter.Write(hoverResult);
                            lsifJsonWriter.Write(Edge.Create(Methods.TextDocumentHoverName, symbolForLinkedResultSetId, hoverResult.GetId(), idFactory));
                        }
                    }
                }
            }

            lsifJsonWriter.Write(Edge.Create("contains", documentVertex.GetId(), rangeVertices, idFactory));

            // Write the folding ranges for the document.
            var foldingRanges = FoldingRangesHandler.GetFoldingRanges(syntaxTree, languageServices, options.BlockStructureOptions, CancellationToken.None);
            var foldingRangeResult = new FoldingRangeResult(foldingRanges, idFactory);
            lsifJsonWriter.Write(foldingRangeResult);
            lsifJsonWriter.Write(Edge.Create(Methods.TextDocumentFoldingRangeName, documentVertex.GetId(), foldingRangeResult.GetId(), idFactory));

            lsifJsonWriter.Write(new Event(Event.EventKind.End, documentVertex.GetId(), idFactory));
            return documentVertex.GetId();
        }

        private static bool IncludeSymbolInReferences(ISymbol symbol)
        {
            // Skip some type of symbols that don't really make sense
            if (symbol.Kind is SymbolKind.ArrayType or
                SymbolKind.Discard or
                SymbolKind.ErrorType)
            {
                return false;
            }

            // If it's a built-in operator, just skip it
            if (symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator })
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
