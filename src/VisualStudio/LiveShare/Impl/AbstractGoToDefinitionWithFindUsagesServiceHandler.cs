// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.Shell;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Typescript uses FAR for goto def as they do not implement the definition service.
    /// So implement goto def for typescript using FAR.
    /// FAR has a UI thread dependency.
    /// </summary>
    internal abstract class AbstractGoToDefinitionWithFindUsagesServiceHandler : ILspRequestHandler<LSP.TextDocumentPositionParams, object, Solution>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceService;
        private readonly ILspSolutionProvider _solutionProvider;

        public AbstractGoToDefinitionWithFindUsagesServiceHandler(IMetadataAsSourceFileService metadataAsSourceService, ILspSolutionProvider solutionProvider)
        {
            _metadataAsSourceService = metadataAsSourceService;
            _solutionProvider = solutionProvider;
        }

        public async Task<object> HandleAsync(LSP.TextDocumentPositionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var document = _solutionProvider.GetDocument(request.TextDocument);
            if (document == null)
            {
                return Array.Empty<LSP.Location>();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var locations = await GetDefinitionsWithFindUsagesServiceAsync(document, position, cancellationToken).ConfigureAwait(false);

            // No definition found - see if we can get metadata as source but that's only applicable for C#\VB.
            if ((locations.Length == 0) && document.SupportsSemanticModel && this._metadataAsSourceService != null)
            {
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (symbol?.Locations.FirstOrDefault().IsInMetadata == true)
                {
                    var declarationFile = await this._metadataAsSourceService.GetGeneratedFileAsync(document.Project, symbol, false, cancellationToken).ConfigureAwait(false);

                    var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    return new LSP.Location[]
                    {
                        new LSP.Location { Uri = new Uri(declarationFile.FilePath), Range = ProtocolConversions.LinePositionToRange(linePosSpan) }
                    };
                }
            }

            return locations.ToArray();
        }

        /// <summary>
        ///  Using the find usages service is more expensive than using the definitions service because a lot of unnecessary information is computed. However,
        ///  TypeScript doesn't provide an <see cref="IGoToDefinitionService"/> implementation that will return definitions so we must use <see cref="IFindUsagesService"/>.
        /// </summary>
        private async Task<ImmutableArray<LSP.Location>> GetDefinitionsWithFindUsagesServiceAsync(Document document, int pos, CancellationToken cancellationToken)
        {
            var findUsagesService = document.Project.LanguageServices.GetRequiredService<IFindUsagesService>();

            var context = new SimpleFindUsagesContext(cancellationToken);

            // Roslyn calls into third party extensions to compute reference results and needs to be on the UI thread to compute results.
            // This is not great for us and ideally we should ask for a Roslyn API where we can make this call without blocking the UI.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await findUsagesService.FindReferencesAsync(document, pos, context).ConfigureAwait(false);

            using var _ = ArrayBuilder<LSP.Location>.GetInstance(out var locations);

            var definitions = context.GetDefinitions();
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    foreach (var docSpan in definition.SourceSpans)
                    {
                        locations.AddIfNotNull(await ProtocolConversions.DocumentSpanToLocationAsync(docSpan, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            return locations.ToImmutable();
        }
    }
}
