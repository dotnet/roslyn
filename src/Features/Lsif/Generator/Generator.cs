// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LspProtocol = Roslyn.LanguageServer.Protocol;
using Methods = Roslyn.LanguageServer.Protocol.Methods;

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
        private const bool DocumentSymbolProvider = true;
        private const bool FoldingRangeProvider = true;
        private const bool DiagnosticProvider = false;

        private static readonly LspProtocol.ClientCapabilities LspClientCapabilities = new()
        {
            TextDocument = new LspProtocol.TextDocumentClientCapabilities()
            {
                Hover = new LspProtocol.HoverSetting()
                {
                    ContentFormat =
                    [
                        LspProtocol.MarkupKind.PlainText,
                        LspProtocol.MarkupKind.Markdown,
                    ]
                }
            }
        };

        private readonly ILsifJsonWriter _lsifJsonWriter;
        private readonly ILogger _logger;
        private readonly IdFactory _idFactory = new IdFactory();

        private Generator(ILsifJsonWriter lsifJsonWriter, ILogger logger)
        {
            _lsifJsonWriter = lsifJsonWriter;
            _logger = logger;
        }

        public static Generator CreateAndWriteCapabilitiesVertex(ILsifJsonWriter lsifJsonWriter, ILogger logger)
        {
            var generator = new Generator(lsifJsonWriter, logger);

            // Pass the set of supported SemanticTokenTypes. Order must match the order used for serialization of
            // semantic tokens array. This array is analogous to the equivalent array in
            // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#textDocument_semanticTokens.
            //
            // Ideally semantic tokens support would use the well-known, common set of token types specified in LSP's
            // SemanticTokenTypes to reduce the number of tokens a particular LSIF consumer must understand, but Roslyn
            // currently employs a large number of custom token types that aren't yet standardized in LSP or LSIF's
            // well-known set so we will pass both LSP and Roslyn custom token types for now.
            var capabilitiesVertex = new Capabilities(
                generator._idFactory,
                HoverProvider,
                DeclarationProvider,
                DefinitionProvider,
                ReferencesProvider,
                TypeDefinitionProvider,
                DocumentSymbolProvider,
                FoldingRangeProvider,
                DiagnosticProvider,
                new SemanticTokensCapabilities(SemanticTokensSchema.LegacyTokensSchemaForLSIF.AllTokenTypes, [SemanticTokenModifiers.Static, SemanticTokenModifiers.Deprecated]));
            generator._lsifJsonWriter.Write(capabilitiesVertex);
            return generator;
        }

        public async Task GenerateForProjectAsync(
            Project project,
            GeneratorOptions options,
            CancellationToken cancellationToken)
        {
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken);
            var projectPath = project.FilePath;
            Contract.ThrowIfNull(projectPath);

            var projectVertex = new Graph.LsifProject(
                kind: GetLanguageKind(compilation.Language),
                ProtocolConversions.CreateAbsoluteUri(projectPath),
                Path.GetFileNameWithoutExtension(projectPath),
                _idFactory);

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

            var documents = new List<Document>();
            await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken))
                documents.Add(document);

            var tasks = new List<Task>();
            foreach (var document in documents)
            {
                // Add a task for each document -- we'll keep them 1:1 for exception reporting later.
                tasks.Add(Task.Run(async () =>
                {
                    // We generate the document contents into an in-memory copy, and then write that out at once at the end. This
                    // allows us to collect everything and avoid a lot of fine-grained contention on the write to the single
                    // LSIF file. Because of the rule that vertices must be written before they're used by an edge, we'll flush any top-
                    // level symbol result sets made first, since the document contents will point to that. Parallel calls to CopyAndEmpty
                    // are allowed and might flush other unrelated stuff at the same time, but there's no harm -- the "causality" ordering
                    // is preserved.
                    var documentWriter = new BatchingLsifJsonWriter(_lsifJsonWriter);
                    var documentId = await GenerateForDocumentAsync(
                        document, options, topLevelSymbolsResultSetTracker, documentWriter, _idFactory, cancellationToken);
                    topLevelSymbolsWriter.FlushToUnderlyingAndEmpty();
                    documentWriter.FlushToUnderlyingAndEmpty();

                    documentIds.Add(documentId);
                }, cancellationToken));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // We ran into some exceptions while processing documents, let's log it along with the document that failed
                var exceptions = new List<Exception>();

                for (var i = 0; i < documents.Count; i++)
                {
                    if (tasks[i].IsFaulted)
                    {
                        var documentExceptionMessage = $"Exception while processing {documents[i].FilePath}";
                        var exception = tasks[i].Exception!.InnerExceptions.Single();
                        exceptions.Add(new Exception(documentExceptionMessage, exception));

                        _logger.LogError(exception, documentExceptionMessage);
                    }
                }

                // Rethrow so we properly report this as a top-level failure
                throw new AggregateException($"Exceptions were thrown while processing documents in {project.FilePath}", exceptions);
            }
            finally
            {
                _lsifJsonWriter.Write(Edge.Create("contains", projectVertex.GetId(), documentIds.ToArray(), _idFactory));
                _lsifJsonWriter.Write(new Event(Event.EventKind.End, projectVertex.GetId(), _idFactory));
            }
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
            Document document,
            GeneratorOptions options,
            IResultSetTracker topLevelSymbolsResultSetTracker,
            ILsifJsonWriter lsifJsonWriter,
            IdFactory idFactory,
            CancellationToken cancellationToken)
        {
            // Create and keep the semantic model alive for this document.  That way all work/services we kick off that
            // use this document can benefit from that single shared model.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken);

            var (uri, contentBase64Encoded) = await GetUriAndContentAsync(document, cancellationToken);

            var documentVertex = new Graph.LsifDocument(uri, GetLanguageKind(semanticModel.Language), contentBase64Encoded, idFactory);
            lsifJsonWriter.Write(documentVertex);
            lsifJsonWriter.Write(new Event(Event.EventKind.Begin, documentVertex.GetId(), idFactory));

            // We will walk the file token-by-token, making a range for each one and then attaching information for it
            var rangeVertices = new List<Id<Graph.Range>>();
            var documentSymbols = new List<RangeBasedDocumentSymbol>();
            await GenerateDocumentRangesAndLinks(document, documentVertex, options, topLevelSymbolsResultSetTracker, lsifJsonWriter, idFactory, rangeVertices, documentSymbols, cancellationToken);
            lsifJsonWriter.Write(Edge.Create("contains", documentVertex.GetId(), rangeVertices, idFactory));
            await GenerateDocumentFoldingRangesAsync(document, documentVertex, options, lsifJsonWriter, idFactory, cancellationToken).ConfigureAwait(false);
            await GenerateSemanticTokensAsync(document, lsifJsonWriter, idFactory, documentVertex);
            GenerateDocumentSymbols(documentSymbols, lsifJsonWriter, idFactory, documentVertex);

            lsifJsonWriter.Write(new Event(Event.EventKind.End, documentVertex.GetId(), idFactory));

            GC.KeepAlive(semanticModel);

            return documentVertex.GetId();
        }

        private static async Task GenerateDocumentFoldingRangesAsync(
            Document document,
            LsifDocument documentVertex,
            GeneratorOptions options,
            ILsifJsonWriter lsifJsonWriter,
            IdFactory idFactory,
            CancellationToken cancellationToken)
        {
            var foldingRanges = await FoldingRangesHandler.GetFoldingRangesAsync(
                document, options.BlockStructureOptions, cancellationToken);
            var foldingRangeResult = new FoldingRangeResult(foldingRanges, idFactory);
            lsifJsonWriter.Write(foldingRangeResult);
            lsifJsonWriter.Write(Edge.Create(Methods.TextDocumentFoldingRangeName, documentVertex.GetId(), foldingRangeResult.GetId(), idFactory));
        }

        private static async Task GenerateDocumentRangesAndLinks(
            Document document,
            LsifDocument documentVertex,
            GeneratorOptions options,
            IResultSetTracker topLevelSymbolsResultSetTracker,
            ILsifJsonWriter lsifJsonWriter,
            IdFactory idFactory,
            List<Id<Graph.Range>> rangeVertices,
            List<RangeBasedDocumentSymbol> documentSymbols,
            CancellationToken cancellationToken)
        {
            var languageServices = document.Project.Services;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken);

            var syntaxTree = semanticModel.SyntaxTree;
            var sourceText = semanticModel.SyntaxTree.GetText(cancellationToken);
            var syntaxFactsService = languageServices.GetRequiredService<ISyntaxFactsService>();
            var semanticFactsService = languageServices.GetRequiredService<ISemanticFactsService>();

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

            foreach (var syntaxToken in syntaxTree.GetRoot(cancellationToken).DescendantTokens(descendIntoTrivia: true))
            {
                var declaredSymbol = semanticFactsService.GetDeclaredSymbol(semanticModel, syntaxToken, cancellationToken);

                // We'll only create the Range vertex once it's needed, but any number of bits of code might create it first,
                // so we'll just make it Lazy.
                var lazyRangeVertex = new Lazy<Graph.Range>(() =>
                {
                    var tagAndFullRangeSpan = declaredSymbol != null ? CreateRangeTagAndContainingSpanForDeclaredSymbol(declaredSymbol, syntaxToken, syntaxTree, syntaxFactsService, cancellationToken) : null;
                    var rangeVertex = Graph.Range.FromTextSpan(syntaxToken.Span, sourceText, tagAndFullRangeSpan?.tag, idFactory);

                    lsifJsonWriter.Write(rangeVertex);
                    rangeVertices.Add(rangeVertex.GetId());

                    if (tagAndFullRangeSpan is not null)
                    {
                        var newDocumentSymbol = new RangeBasedDocumentSymbol(rangeVertex.GetId(), tagAndFullRangeSpan.Value.fullRange);
                        RangeBasedDocumentSymbol.AddNestedFromDocumentOrderTraversal(documentSymbols, newDocumentSymbol);
                    }

                    return rangeVertex;
                }, LazyThreadSafetyMode.None);

                ISymbol? referencedSymbol = null;

                if (syntaxFactsService.IsBindableToken(syntaxToken))
                {
                    var bindableParent = syntaxFactsService.TryGetBindableParent(syntaxToken);

                    if (bindableParent != null)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(bindableParent, cancellationToken);
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
                    var symbolForLinkedResultSet = (declaredSymbol ?? referencedSymbol)!.GetOriginalUnreducedDefinition();
                    var symbolForLinkedResultSetId = symbolResultsTracker.GetResultSetIdForSymbol(symbolForLinkedResultSet);
                    lsifJsonWriter.Write(Edge.Create("next", lazyRangeVertex.Value.GetId(), symbolForLinkedResultSetId, idFactory));

                    if (declaredSymbol != null)
                    {
                        var definitionResultsId = symbolResultsTracker.GetResultIdForSymbol(declaredSymbol, Methods.TextDocumentDefinitionName, static idFactory => new DefinitionResult(idFactory));
                        lsifJsonWriter.Write(new Item(definitionResultsId.As<DefinitionResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId(), idFactory));

                        // If this declared symbol also implements an interface member, we count this as a definition of the interface member as well.
                        // Note in C# there are estoeric cases where a method can implement an interface member even though the containing type does not
                        // implement the interface, for example in this case:
                        //
                        //     interface I { void M(); }
                        //     class Base { public void M() { } }
                        //     class Derived : Base, I { }
                        //
                        // We don't worry about supporting these cases here.
                        var implementedMembers = declaredSymbol.ExplicitOrImplicitInterfaceImplementations();

                        foreach (var implementedMember in implementedMembers)
                            MarkImplementationOfSymbol(implementedMember);

                        // If this overrides a method, we'll also mark it the same way. We want to chase to the base virtual method, skipping over intermediate
                        // methods so that way all overrides of the same method point to the same virtual method
                        if (declaredSymbol.IsOverride)
                        {
                            var overridenMember = declaredSymbol.GetOverriddenMember();

                            while (overridenMember?.GetOverriddenMember() != null)
                                overridenMember = overridenMember.GetOverriddenMember();

                            if (overridenMember != null)
                                MarkImplementationOfSymbol(overridenMember);
                        }

                        void MarkImplementationOfSymbol(ISymbol baseMember)
                        {
                            // First we create a definition link for the reference results for the base member
                            var referenceResultsId = symbolResultsTracker.GetResultSetReferenceResultId(baseMember.OriginalDefinition);
                            lsifJsonWriter.Write(new Item(referenceResultsId.As<ReferenceResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId(), idFactory, property: "definitions"));

                            // Then also link the result set for the method to the moniker that it implements
                            referenceResultsId = symbolResultsTracker.GetResultSetReferenceResultId(declaredSymbol.OriginalDefinition);
                            var implementedMemberMoniker = symbolResultsTracker.GetMoniker(baseMember.OriginalDefinition, semanticModel.Compilation);
                            lsifJsonWriter.Write(new Item(referenceResultsId.As<ReferenceResult, Vertex>(), implementedMemberMoniker, documentVertex.GetId(), idFactory, property: "referenceLinks"));
                        }
                    }

                    if (referencedSymbol != null)
                    {
                        // Create the link from the references back to this range. Note: this range can be reference to a
                        // symbol but the range can point a different symbol's resultSet. This can happen if the token is
                        // both a definition of a symbol (where we will point to the definition) but also a reference to some
                        // other symbol.
                        var referenceResultsId = symbolResultsTracker.GetResultSetReferenceResultId(referencedSymbol.GetOriginalUnreducedDefinition());
                        lsifJsonWriter.Write(new Item(referenceResultsId.As<ReferenceResult, Vertex>(), lazyRangeVertex.Value.GetId(), documentVertex.GetId(), idFactory, property: "references"));
                    }

                    // Write hover information for the symbol, if edge has not already been added.
                    // 'textDocument/hover' edge goes from the symbol ResultSet vertex to the hover result
                    // See https://github.com/Microsoft/language-server-protocol/blob/main/indexFormat/specification.md#resultset for an example.
                    if (symbolResultsTracker.ResultSetNeedsInformationalEdgeAdded(symbolForLinkedResultSet, Methods.TextDocumentHoverName))
                    {
                        var hover = await HoverHandler.GetHoverAsync(
                            document, syntaxToken.SpanStart, options.SymbolDescriptionOptions, LspClientCapabilities, cancellationToken);
                        if (hover != null)
                        {
                            var hoverResult = new HoverResult(hover, idFactory);
                            lsifJsonWriter.Write(hoverResult);
                            lsifJsonWriter.Write(Edge.Create(Methods.TextDocumentHoverName, symbolForLinkedResultSetId, hoverResult.GetId(), idFactory));
                        }
                    }
                }
            }
        }

        private static (DefinitionRangeTag tag, TextSpan fullRange)? CreateRangeTagAndContainingSpanForDeclaredSymbol(ISymbol declaredSymbol, SyntaxToken syntaxToken, SyntaxTree syntaxTree, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            // Tuples fields and anonymous type are considered as declaring something, but we don't want to create document symbols for them
            if (declaredSymbol.IsTupleField() || declaredSymbol.IsAnonymousTypeProperty())
                return null;

            // Find the syntax node that declared the symbol in the tree we're processing
            var syntaxReference = declaredSymbol.DeclaringSyntaxReferences.FirstOrDefault(predicate: static (r, arg) => r.SyntaxTree == arg.syntaxTree && r.Span.Contains(arg.SpanStart), arg: (syntaxTree, syntaxToken.SpanStart));
            var syntaxNode = syntaxReference?.GetSyntax(cancellationToken);

            if (syntaxNode is null)
                return null;

            // The containing range is supposed to be "the full range of the declaration not including leading/trailing whitespace, but everything else,
            // e.g. comments and code", so produce our start/end points looking through trivia
            var firstNonWhitespaceTrivia = syntaxNode.GetLeadingTrivia().FirstOrNull(predicate: static (t, syntaxFacts) => !syntaxFacts.IsWhitespaceOrEndOfLineTrivia(t), arg: syntaxFacts);
            var fullRangeStart = firstNonWhitespaceTrivia?.SpanStart ?? syntaxNode.SpanStart;

            var lastNonWhitespaceTrivia = syntaxNode.GetTrailingTrivia().Reverse().FirstOrNull(predicate: static (t, syntaxFacts) => !syntaxFacts.IsWhitespaceOrEndOfLineTrivia(t), arg: syntaxFacts);
            var fullRangeEnd = lastNonWhitespaceTrivia?.Span.End ?? syntaxNode.Span.End;

            var fullRangeSpan = TextSpan.FromBounds(fullRangeStart, fullRangeEnd);
            var fullRange = ProtocolConversions.TextSpanToRange(fullRangeSpan, syntaxTree.GetText(cancellationToken));
            var symbolKind = ProtocolConversions.GlyphToSymbolKind(declaredSymbol.GetGlyph());

            return (new DefinitionRangeTag(syntaxToken.Text, symbolKind, fullRange), fullRangeSpan);
        }

        private static async Task<(Uri uri, string? contentBase64Encoded)> GetUriAndContentAsync(
            Document document, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(document.FilePath);

            string? contentBase64Encoded = null;
            Uri uri;

            if (document is SourceGeneratedDocument)
            {
                var text = await document.GetValueTextAsync(cancellationToken);

                // We always use UTF-8 encoding when writing out file contents, as that's expected by LSIF implementations.
                // TODO: when we move to .NET Core, is there a way to reduce allocations here?
                contentBase64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(text.ToString()));

                // There is a triple slash here, so the "host" portion of the URI is empty, similar to
                // how file URIs work.
                uri = ProtocolConversions.CreateUriFromSourceGeneratedFilePath(document.FilePath);
            }
            else
            {
                uri = ProtocolConversions.CreateAbsoluteUri(document.FilePath);
            }

            return (uri, contentBase64Encoded);
        }

        private static async Task GenerateSemanticTokensAsync(
            Document document,
            ILsifJsonWriter lsifJsonWriter,
            IdFactory idFactory,
            LsifDocument documentVertex)
        {
            // Compute colorization data.
            //
            // Unlike the mainline LSP scenario, where we control both the syntactic colorizer (in-proc syntax tagger)
            // and the semantic colorizer (LSP semantic tokens) LSIF is more likely to be consumed by clients which may
            // have different syntactic classification behavior than us, resulting in missing colors. To avoid this, we
            // include syntax tokens in the generated data.
            var data = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                // Just get the pure-lsp semantic tokens here.
                document,
                spans: [],
                supportsVisualStudioExtensions: true,
                options: Classification.ClassificationOptions.Default,
                cancellationToken: CancellationToken.None);

            var semanticTokensResult = new SemanticTokensResult(new SemanticTokens { Data = data }, idFactory);
            var semanticTokensEdge = Edge.Create(Methods.TextDocumentSemanticTokensFullName, documentVertex.GetId(), semanticTokensResult.GetId(), idFactory);
            lsifJsonWriter.Write(semanticTokensResult);
            lsifJsonWriter.Write(semanticTokensEdge);
        }

        private static void GenerateDocumentSymbols(
            List<RangeBasedDocumentSymbol> documentSymbols,
            ILsifJsonWriter lsifJsonWriter,
            IdFactory idFactory,
            LsifDocument documentVertex)
        {
            var documentSymbolResult = new DocumentSymbolResult(documentSymbols, idFactory);
            lsifJsonWriter.Write(documentSymbolResult);
            lsifJsonWriter.Write(Edge.Create(Methods.TextDocumentDocumentSymbolName, documentVertex.GetId(), documentSymbolResult.GetId(), idFactory));
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
