// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Protocol.LanguageServices.Extensions;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using VSLocation = Microsoft.VisualStudio.LanguageServer.Protocol.Location;

namespace Microsoft.CodeAnalysis.Protocol.LanguageServices
{
    /// <summary>
    /// This is the roslyn language service.
    ///
    /// This glues the LSP requests and solution to 
    /// Roslyn (and services) and outputs LSP types.
    /// </summary>
    public sealed class RoslynLanguageService
    {
        // for now, it is null but later we might use this to provide info on types on dlls
        private readonly IMetadataAsSourceFileService _metadataAsSourceService = null;

        // TODO - Move hierarchicalDocumentSymbolSupport to client capabilities.
        // https://github.com/dotnet/roslyn/projects/45#card-20033973
        private readonly bool _hierarchicalDocumentSymbolSupport;
        private readonly ClientCapabilities _clientCapabilities;

        public RoslynLanguageService(ClientCapabilities clientCapabilities, bool hierarchicalDocumentSymbolSupport)
        {
            _hierarchicalDocumentSymbolSupport = hierarchicalDocumentSymbolSupport;
            _clientCapabilities = clientCapabilities;
        }

        public ServerCapabilities GetServerCapabilities()
        {
            return new ServerCapabilities
            {
                DefinitionProvider = true,
                ReferencesProvider = true,
                DocumentHighlightProvider = true,
                DocumentSymbolProvider = true,
                WorkspaceSymbolProvider = true,
                HoverProvider = true,
            };
        }

        /// <summary>
        /// Answers a document symbols request by returning a list of symbols in the document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_documentSymbol
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the document to get symbols from.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of symbols in the document.</returns>
        public async Task<object[]> GetDocumentSymbolsAsync(Solution solution, DocumentSymbolParams request, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<SymbolInformation>();
            }

            var symbols = ArrayBuilder<object>.GetInstance();

            var navBarService = document.Project.LanguageServices.GetService<INavigationBarItemService>();
            var navBarItems = await navBarService.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            if (navBarItems.Count == 0)
            {
                return symbols.ToArrayAndFree();
            }

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // TODO - Return more than 2 levels of symbols.
            // https://github.com/dotnet/roslyn/projects/45#card-20033869
            return await CreateDocumentSymbolOrSymbolInformations().ConfigureAwait(false);

            // local functions
            async Task<object[]> CreateDocumentSymbolOrSymbolInformations()
            {
                if (_hierarchicalDocumentSymbolSupport)
                {
                    foreach (var item in navBarItems)
                    {
                        // only top level ones
                        symbols.Add(await CreateDocumentSymbolAsync(item).ConfigureAwait(false));
                    }
                }
                else
                {
                    foreach (var item in navBarItems)
                    {
                        symbols.Add(GetSymbolLocation(item, containerName: null));

                        foreach (var childItem in item.ChildItems)
                        {
                            symbols.Add(GetSymbolLocation(childItem, item.Text));
                        }
                    }
                }

                var result = symbols.Where(s => s != null).ToArray();
                symbols.Free();
                return result;
            }

            SymbolInformation GetSymbolLocation(NavigationBarItem item, string containerName)
            {
                if (item.Spans.Count == 0)
                {
                    return null;
                }

                var location = GetLocation(item);

                if (location == null)
                {
                    return Create(item.Spans.First());
                }

                return Create(location.SourceSpan);

                SymbolInformation Create(TextSpan span)
                {
                    return new SymbolInformation
                    {
                        Name = item.Text,
                        Location = new VSLocation
                        {
                            Uri = document.ToUri(),
                            Range = span.ToRange(text),
                        },
                        Kind = item.Glyph.ToSymbolKind(),
                        ContainerName = containerName,
                    };
                }
            }

            async Task<DocumentSymbol> CreateDocumentSymbolAsync(NavigationBarItem item)
            {
                // it is actually symbol location getter. but anyway.
                var location = GetLocation(item);
                if (location == null)
                {
                    return null;
                }

                var symbol = await GetSymbolAsync(location).ConfigureAwait(false);
                if (symbol == null)
                {
                    return null;
                }

                return new DocumentSymbol
                {
                    Name = symbol.Name,
                    Detail = item.Text,
                    Kind = item.Glyph.ToSymbolKind(),
                    Deprecated = symbol.GetAttributes().Any(x => x.AttributeClass.MetadataName == "ObsoleteAttribute"),
                    Range = item.Spans.First().ToRange(text),
                    SelectionRange = location.SourceSpan.ToRange(text),
                    Children = await GetChildrenAsync(item.ChildItems).ConfigureAwait(false),
                };
            }

            Location GetLocation(NavigationBarItem item)
            {
                if (!(item is NavigationBarSymbolItem symbolItem))
                {
                    return null;
                }

                var symbols = symbolItem.NavigationSymbolId.Resolve(compilation, cancellationToken: cancellationToken);
                var symbol = symbols.Symbol;

                if (symbol == null)
                {
                    if (symbolItem.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
                    {
                        symbol = symbols.CandidateSymbols[symbolItem.NavigationSymbolIndex.Value];
                    }
                    else
                    {
                        return null;
                    }
                }

                var location = symbol.Locations.FirstOrDefault(l => l.SourceTree.Equals(tree));
                return location ?? symbol.Locations.FirstOrDefault();
            }

            async Task<List<DocumentSymbol>> GetChildrenAsync(IEnumerable<NavigationBarItem> items)
            {
                var list = new List<DocumentSymbol>();
                foreach (var item in items)
                {
                    list.Add(await CreateDocumentSymbolAsync(item).ConfigureAwait(false));
                }

                return list;
            }

            async Task<ISymbol> GetSymbolAsync(Location location)
            {
                var model = compilation.GetSemanticModel(location.SourceTree);
                var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var node = root.FindNode(location.SourceSpan);

                while (node != null)
                {
                    var symbol = model.GetDeclaredSymbol(node);
                    if (symbol != null)
                    {
                        return symbol;
                    }

                    node = node.Parent;
                }

                return null;
            }
        }

        /// <summary>
        /// Answers a workspace symbols request by providing a list of symbols found in a given workspace.
        /// https://microsoft.github.io/language-server-protocol/specification#workspace_symbol
        /// </summary>
        /// <param name="solution">the current solution.</param>
        /// <param name="request">the workspace request with the query to invoke.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of symbols in the workspace.</returns>
        public async Task<SymbolInformation[]> GetWorkspaceSymbolsAsync(Solution solution, WorkspaceSymbolParams request, CancellationToken cancellationToken)
        {
            var symbols = ArrayBuilder<SymbolInformation>.GetInstance();

            if (solution == null)
            {
                return symbols.ToArrayAndFree();
            }

            var searchTasks = solution.Projects.Select(
                p => Task.Run(() => SearchProjectAsync(p), cancellationToken)).ToArray();

            await Task.WhenAll(searchTasks).ConfigureAwait(false);

            return symbols.ToArrayAndFree();

            // local functions
            async Task SearchProjectAsync(Project project)
            {
                var searchService = project.LanguageServices.GetService<INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate>();
                if (searchService != null)
                {
                    // TODO - Update Kinds Provided to return all necessary symbols.
                    // https://github.com/dotnet/roslyn/projects/45#card-20033822
                    var items = await searchService.SearchProjectAsync(
                        project,
                        ImmutableArray<Document>.Empty,
                        request.Query,
                        searchService.KindsProvided,
                        cancellationToken).ConfigureAwait(false);

                    foreach (var item in items)
                    {
                        var symbolInfo = new SymbolInformation
                        {
                            Name = item.Name,
                            Kind = item.Kind.GetKind(),
                            Location = await item.NavigableItem.Document.ToLocationAsync(item.NavigableItem.SourceSpan, cancellationToken).ConfigureAwait(false),
                        };

                        lock (symbols)
                        {
                            symbols.Add(symbolInfo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Answers a Hover request by returning the quick info at the requested location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
        /// </summary>
        /// <param name="solution">the solution containing any documents in the request.</param>
        /// <param name="request">the hover requesst.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the Hover using MarkupContent.</returns>
        public async Task<Hover> GetHoverAsync(Solution solution, TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return null;
            }

            var position = await document.GetPositionAsync(request.Position.ToLinePosition(), cancellationToken).ConfigureAwait(false);

            var quickInfoService = document.Project.LanguageServices.GetService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new Hover
            {
                Range = info.Span.ToRange(text),
                Contents = new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = GetMarkdownString(info)
                }
            };

            // local functions
            // TODO - This should return correctly formatted markdown from quick info.
            // https://github.com/dotnet/roslyn/projects/45#card-20033878
            static string GetMarkdownString(QuickInfoItem info)
            {
                var stringBuilder = new StringBuilder();
                var description = info.Sections.FirstOrDefault(s => QuickInfoSectionKinds.Description.Equals(s.Kind))?.Text ?? string.Empty;
                var documentation = info.Sections.FirstOrDefault(s => QuickInfoSectionKinds.DocumentationComments.Equals(s.Kind))?.Text ?? string.Empty;

                if (!string.IsNullOrEmpty(description))
                {
                    stringBuilder.Append(description);
                    if (!string.IsNullOrEmpty(documentation))
                    {
                        stringBuilder.Append("\r\n> ").Append(documentation);
                    }
                }

                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Answers a goto definition request by returning the location for a given symbol definition.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_definition
        /// </summary>
        /// <param name="solution">the solution containing the request.</param>
        /// <param name="request">the document position of the symbol to go to.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location of a given symbol.</returns>
        public Task<VSLocation[]> GoToDefinitionAsync(Solution solution, TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return GetDefinitionAsync(solution, request, typeOnly: false, cancellationToken);
        }

        /// <summary>
        /// Answers a goto type definition request by returning the location of a given type definition.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_typeDefinition
        /// </summary>
        /// <param name="solution">the solution containing the request.</param>
        /// <param name="request">the document position of the type to go to.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location of a type definition.</returns>
        public Task<VSLocation[]> GoToTypeDefinitionAsync(Solution solution, TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            return GetDefinitionAsync(solution, request, typeOnly: true, cancellationToken);
        }

        /// <summary>
        /// Answers a find references request by returning the location of references to the given symbol.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_references
        /// </summary>
        /// <param name="solution">the solution containing the symbol.</param>
        /// <param name="request">the request symbol document location.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of locations of references to the given symbol.</returns>
        public async Task<VSLocation[]> FindAllReferencesAsync(Solution solution, ReferenceParams request, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<VSLocation>.GetInstance();

            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();
            var position = await document.GetPositionAsync(request.Position.ToLinePosition(), cancellationToken).ConfigureAwait(false);

            var context = new SimpleFindUsagesContext(cancellationToken);

            await findUsagesService.FindReferencesAsync(document, position, context).ConfigureAwait(false);

            if (request.Context.IncludeDeclaration)
            {
                foreach (var definition in context.GetDefinitions())
                {
                    foreach (var docSpan in definition.SourceSpans)
                    {
                        locations.Add(await docSpan.ToLocationAsync(cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            foreach (var reference in context.GetReferences())
            {
                locations.Add(await reference.SourceSpan.ToLocationAsync(cancellationToken).ConfigureAwait(false));
            }

            return locations.ToArrayAndFree();
        }

        /// <summary>
        /// Answers a goto implementation request by returning the implementation location(s) of a given symbol.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_implementation
        /// </summary>
        /// <param name="solution">the solution containing the request document.</param>
        /// <param name="request">the request document symbol location.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the location(s) of the implementations of the symbol.</returns>
        public async Task<VSLocation[]> GotoImplementationAsync(Solution solution, TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<VSLocation>.GetInstance();

            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();
            var position = await document.GetPositionAsync(request.Position.ToLinePosition(), cancellationToken).ConfigureAwait(false);

            var context = new SimpleFindUsagesContext(cancellationToken);

            await findUsagesService.FindImplementationsAsync(document, position, context).ConfigureAwait(false);

            foreach (var sourceSpan in context.GetDefinitions().SelectMany(definition => definition.SourceSpans))
            {
                locations.Add(await sourceSpan.ToLocationAsync(cancellationToken).ConfigureAwait(false));
            }

            return locations.ToArrayAndFree();
        }

        /// <summary>
        /// Answers a document highlights request by returning the highlights for a given document location.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_documentHighlight
        /// </summary>
        /// <param name="solution">the solution containing the request document.</param>
        /// <param name="request">the request document location.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the highlights in the document for the given document location.</returns>
        public async Task<DocumentHighlight[]> GetDocumentHighlightAsync(Solution solution, TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            var docHighlights = ArrayBuilder<DocumentHighlight>.GetInstance();

            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return docHighlights.ToArrayAndFree();
            }

            var documentHighlightService = document.Project.LanguageServices.GetService<IDocumentHighlightsService>();
            var position = await document.GetPositionAsync(request.Position.ToLinePosition(), cancellationToken).ConfigureAwait(false);

            var highlights = await documentHighlightService.GetDocumentHighlightsAsync(
                document,
                position,
                ImmutableHashSet.Create(document),
                cancellationToken).ConfigureAwait(false);

            if (!highlights.IsDefaultOrEmpty)
            {
                // LSP requests are only for a single document. So just get the highlights for the requested document.
                var highlightsForDocument = highlights.FirstOrDefault(h => h.Document.Id == document.Id);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                docHighlights.AddRange(highlightsForDocument.HighlightSpans.Select(h =>
                {
                    return new DocumentHighlight
                    {
                        Range = h.TextSpan.ToRange(text),
                        Kind = h.Kind.ToDocumentHighlightKind(),
                    };
                }));
            }

            return docHighlights.ToArrayAndFree();
        }

        /// <summary>
        /// Answers a folding range request by returning all folding ranges in a given document.
        /// https://microsoft.github.io/language-server-protocol/specification#textDocument_foldingRange
        /// </summary>
        /// <param name="solution">the solution containing the document.</param>
        /// <param name="request">the request document.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>a list of folding ranges in the document.</returns>
        public async Task<FoldingRange[]> GetFoldingRangeAsync(Solution solution, FoldingRangeParams request, CancellationToken cancellationToken)
        {
            var foldingRanges = ArrayBuilder<FoldingRange>.GetInstance();

            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var blockStructureService = document.Project.LanguageServices.GetService<BlockStructureService>();
            if (blockStructureService == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var blockStructure = await blockStructureService.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);
            if (blockStructure == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var span in blockStructure.Spans)
            {
                if (!span.IsCollapsible)
                {
                    continue;
                }

                var linePositionSpan = text.Lines.GetLinePositionSpan(span.TextSpan);
                foldingRanges.Add(new FoldingRange()
                {
                    StartLine = linePositionSpan.Start.Line,
                    StartCharacter = linePositionSpan.Start.Character,
                    EndLine = linePositionSpan.End.Line,
                    EndCharacter = linePositionSpan.End.Character,
                    Kind = ConvertToWellKnownBlockType(span.Type),
                });
            }

            return foldingRanges.ToArrayAndFree();

            // local functions
            // TODO - Figure out which blocks should be returned as a folding range (and what kind).
            // https://github.com/dotnet/roslyn/projects/45#card-20049168
            static string ConvertToWellKnownBlockType(string kind)
            {
                switch (kind)
                {
                    case BlockTypes.Comment: return FoldingRangeKind.Comment;
                    case BlockTypes.Imports: return FoldingRangeKind.Imports;
                    case BlockTypes.PreprocessorRegion: return FoldingRangeKind.Region;
                    default: return null;
                }
            }
        }

        private async Task<VSLocation[]> GetDefinitionAsync(Solution solution, TextDocumentPositionParams request, bool typeOnly, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<VSLocation>.GetInstance();

            var document = solution.GetDocument(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var position = await document.GetPositionAsync(request.Position.ToLinePosition(), cancellationToken).ConfigureAwait(false);

            var definitionService = document.Project.LanguageServices.GetService<IGoToDefinitionService>();

            var definitions = await definitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (definitions != null && definitions.Count() > 0)
            {
                foreach (var definition in definitions)
                {
                    if (!ShouldInclude(definition))
                    {
                        continue;
                    }

                    var definitionText = await definition.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    locations.Add(new VSLocation
                    {
                        Uri = definition.Document.ToUri(),
                        Range = definition.SourceSpan.ToRange(definitionText),
                    });
                }
            }

            // No definition found - see if we can get metadata as source but that's only applicable for C#\VB.
            else if (document.SupportsSemanticModel && _metadataAsSourceService != null)
            {
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (symbol.Locations.First().IsInMetadata)
                {
                    if (!typeOnly || symbol is ITypeSymbol)
                    {
                        var declarationFile = await _metadataAsSourceService.GetGeneratedFileAsync(document.Project, symbol, false, cancellationToken).ConfigureAwait(false);

                        var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                        locations.Add(new VSLocation
                        {
                            Uri = new Uri(declarationFile.FilePath),
                            Range = linePosSpan.ToRange(),
                        });
                    }
                }
            }

            return locations.ToArrayAndFree();

            // local functions
            bool ShouldInclude(INavigableItem item)
            {
                if (!typeOnly)
                {
                    return true;
                }

                switch (item.Glyph)
                {
                    case Glyph.ClassPublic:
                    case Glyph.ClassProtected:
                    case Glyph.ClassPrivate:
                    case Glyph.ClassInternal:
                    case Glyph.DelegatePublic:
                    case Glyph.DelegateProtected:
                    case Glyph.DelegatePrivate:
                    case Glyph.DelegateInternal:
                    case Glyph.EnumPublic:
                    case Glyph.EnumProtected:
                    case Glyph.EnumPrivate:
                    case Glyph.EnumInternal:
                    case Glyph.EventPublic:
                    case Glyph.EventProtected:
                    case Glyph.EventPrivate:
                    case Glyph.EventInternal:
                    case Glyph.InterfacePublic:
                    case Glyph.InterfaceProtected:
                    case Glyph.InterfacePrivate:
                    case Glyph.InterfaceInternal:
                    case Glyph.ModulePublic:
                    case Glyph.ModuleProtected:
                    case Glyph.ModulePrivate:
                    case Glyph.ModuleInternal:
                    case Glyph.StructurePublic:
                    case Glyph.StructureProtected:
                    case Glyph.StructurePrivate:
                    case Glyph.StructureInternal:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
