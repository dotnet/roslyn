// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Definitions;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Definitions
{
    [ExportStatelessXamlLspService(typeof(GoToDefinitionHandler)), Shared]
    [Method(Methods.TextDocumentDefinitionName)]
    internal class GoToDefinitionHandler : ILspServiceRequestHandler<TextDocumentPositionParams, LSP.Location[]>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
            _globalOptions = globalOptions;
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public async Task<LSP.Location[]> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var locations = new ConcurrentBag<LSP.Location>();

            var document = context.Document;
            if (document == null)
            {
                return locations.ToArray();
            }

            var xamlGoToDefinitionService = document.Project.Services.GetService<IXamlGoToDefinitionService>();
            if (xamlGoToDefinitionService == null)
            {
                return locations.ToArray();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var definitions = await xamlGoToDefinitionService.GetDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var definition in definitions)
            {
                var task = Task.Run(async () =>
                {
                    foreach (var location in await this.GetLocationsAsync(definition, context, cancellationToken).ConfigureAwait(false))
                    {
                        locations.Add(location);
                    }
                }, cancellationToken);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return locations.ToArray();
        }

        private async Task<LSP.Location[]> GetLocationsAsync(XamlDefinition definition, RequestContext context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<LSP.Location>.GetInstance(out var locations);

            if (definition is XamlSourceDefinition sourceDefinition)
            {
                locations.AddIfNotNull(await GetSourceDefinitionLocationAsync(sourceDefinition, context, cancellationToken).ConfigureAwait(false));
            }
            else if (definition is XamlSymbolDefinition symbolDefinition)
            {
                locations.AddRange(await GetSymbolDefinitionLocationsAsync(symbolDefinition, context, _metadataAsSourceFileService, _globalOptions, cancellationToken).ConfigureAwait(false));
            }
            else
            {
                throw new InvalidOperationException($"Unexpected {nameof(XamlDefinition)} Type");
            }

            return locations.ToArray();
        }

        private static async Task<LSP.Location?> GetSourceDefinitionLocationAsync(XamlSourceDefinition sourceDefinition, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(sourceDefinition.FilePath);

            if (sourceDefinition.Span != null)
            {
                // If the Span is not null, use the span.
                var document = context.Solution?.GetDocuments(ProtocolConversions.CreateAbsoluteUri(sourceDefinition.FilePath)).FirstOrDefault();
                if (document != null)
                {
                    return await ProtocolConversions.TextSpanToLocationAsync(
                                                document,
                                                sourceDefinition.Span.Value,
                                                isStale: false,
                                                cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Cannot find the file in solution. This is probably a file lives outside of the solution like generic.xaml
                    // which lives in the Windows SDK folder. Try getting the SourceText from the file path.
                    using var fileStream = new FileStream(sourceDefinition.FilePath, FileMode.Open, FileAccess.Read);
                    var sourceText = SourceText.From(fileStream);
                    return new LSP.Location
                    {
                        Uri = new Uri(sourceDefinition.FilePath),
                        Range = ProtocolConversions.TextSpanToRange(sourceDefinition.Span.Value, sourceText)
                    };
                }
            }
            else
            {
                // We should have the line and column, so use them to build the LSP Range.
                var position = new Position(sourceDefinition.Line, sourceDefinition.Column);

                return new LSP.Location
                {
                    Uri = new Uri(sourceDefinition.FilePath),
                    Range = new LSP.Range() { Start = position, End = position }
                };
            }
        }

        private static async Task<LSP.Location[]> GetSymbolDefinitionLocationsAsync(XamlSymbolDefinition symbolDefinition, RequestContext context, IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbolDefinition.Symbol);

            using var _ = ArrayBuilder<LSP.Location>.GetInstance(out var locations);

            var symbol = symbolDefinition.Symbol;

            var items = NavigableItemFactory.GetItemsFromPreferredSourceLocations(context.Solution, symbol, displayTaggedParts: null, cancellationToken);
            if (items.Any())
            {
                RoslynDebug.AssertNotNull(context.Solution);
                foreach (var item in items)
                {
                    var document = await item.Document.GetRequiredDocumentAsync(context.Solution, cancellationToken).ConfigureAwait(false);
                    var location = await ProtocolConversions.TextSpanToLocationAsync(
                        document, item.SourceSpan, item.IsStale, cancellationToken).ConfigureAwait(false);
                    locations.AddIfNotNull(location);
                }
            }
            else
            {
                if (metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
                {
                    var workspace = context.Workspace;
                    var project = context.Document?.GetCodeProject();
                    if (workspace != null && project != null)
                    {
                        var options = globalOptions.GetMetadataAsSourceOptions(project.Services);
                        var declarationFile = await metadataAsSourceFileService.GetGeneratedFileAsync(workspace, project, symbol, signaturesOnly: true, options, cancellationToken).ConfigureAwait(false);
                        var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                        locations.Add(new LSP.Location
                        {
                            Uri = new Uri(declarationFile.FilePath),
                            Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                        });
                    }
                }
            }

            return locations.ToArray();
        }
    }
}
