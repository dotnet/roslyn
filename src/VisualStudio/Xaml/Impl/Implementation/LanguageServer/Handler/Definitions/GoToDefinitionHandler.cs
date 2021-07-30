// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Definitions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Definitions
{
    [ExportLspRequestHandlerProvider(StringConstants.XamlLanguageName), Shared]
    [ProvidesMethod(Methods.TextDocumentDefinitionName)]
    internal class GoToDefinitionHandler : AbstractStatelessRequestHandler<TextDocumentPositionParams, LSP.Location[]>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
        }

        public override string Method => Methods.TextDocumentDefinitionName;

        public override bool MutatesSolutionState => false;

        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public override async Task<LSP.Location[]> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = context.Document;
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var xamlGoToDefinitionService = document.Project.LanguageServices.GetService<IXamlGoToDefinitionService>();
            if (xamlGoToDefinitionService == null)
            {
                return locations.ToArrayAndFree();
            }

            var definitions = await xamlGoToDefinitionService.GetDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            foreach (var definition in definitions)
            {
                locations.AddRange(await this.GetLocationsAsync(definition, context, cancellationToken).ConfigureAwait(false));
            }

            return locations.ToArrayAndFree();
        }

        private async Task<LSP.Location[]> GetLocationsAsync(XamlDefinition definition, RequestContext context, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            if (definition is XamlSourceDefinition sourceDefinition)
            {
                Contract.ThrowIfNull(sourceDefinition.FilePath);

                var document = context.Solution?.GetDocuments(ProtocolConversions.GetUriFromFilePath(sourceDefinition.FilePath)).FirstOrDefault();
                if (document != null)
                {
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var span = sourceDefinition.GetTextSpan(sourceText);
                    if (span != null)
                    {
                        var location = await ProtocolConversions.TextSpanToLocationAsync(
                                                    document,
                                                    span.Value,
                                                    isStale: false,
                                                    cancellationToken).ConfigureAwait(false);
                        locations.AddIfNotNull(location);
                    }
                }
                else
                {
                    // Cannot find the file in solution. This is probably a file lives outside of the solution like generic.xaml
                    // which lives in the Windows SDK folder. Try getting the SourceText from the file path.
                    using var fileStream = new FileStream(sourceDefinition.FilePath, FileMode.Open, FileAccess.Read);
                    var sourceText = SourceText.From(fileStream);
                    var span = sourceDefinition.GetTextSpan(sourceText);
                    if (span != null)
                    {
                        locations.Add(new LSP.Location
                        {
                            Uri = new Uri(sourceDefinition.FilePath),
                            Range = ProtocolConversions.TextSpanToRange(span.Value, sourceText),
                        });
                    }
                }
            }
            else if (definition is XamlSymbolDefinition symbolDefinition)
            {
                Contract.ThrowIfNull(symbolDefinition.Symbol);
                var symbol = symbolDefinition.Symbol;

                if (symbol.Locations.First().IsInMetadata && _metadataAsSourceFileService != null)
                {
                    var project = context.Document?.GetCodeProject();
                    if (project != null)
                    {
                        var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol, allowDecompilation: false, cancellationToken).ConfigureAwait(false);
                        var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                        locations.Add(new LSP.Location
                        {
                            Uri = new Uri(declarationFile.FilePath),
                            Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                        });
                    }
                }
                else
                {
                    var items = NavigableItemFactory.GetItemsFromPreferredSourceLocations(context.Solution, symbol, displayTaggedParts: null, cancellationToken);
                    foreach (var item in items)
                    {
                        var location = await ProtocolConversions.TextSpanToLocationAsync(
                            item.Document, item.SourceSpan, item.IsStale, cancellationToken).ConfigureAwait(false);
                        locations.AddIfNotNull(location);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Unexpected XamlDefinition Type");
            }

            return locations.ToArrayAndFree();
        }
    }
}
