// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.VisualStudio.Shell;
using Microsoft.CodeAnalysis.LanguageServer;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Handler for a goto defintion request. This can return files outside the shared
    /// folder cone. The HostProtocolConverter handles converting files outside the shared
    /// cone to the vslsexternal scheme.
    /// This lives in external access because it has a dependency on FAR, which requires the UI thread.
    /// </summary>
    internal class GotoDefinitionHandler : ILspRequestHandler<LSP.TextDocumentPositionParams, object, Solution>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceService;

        // These languages don't provide an IGoToDefinitionService implementation that will return definitions. We'll use IFindUsagesService instead.
        private static readonly string[] s_findUsagesServiceLanguages = new string[] { LiveShareConstants.TypeScriptLanguageName };

        public GotoDefinitionHandler([Import(AllowDefault = true)] IMetadataAsSourceFileService metadataAsSourceService)
        {
            this._metadataAsSourceService = metadataAsSourceService;
        }

        public async Task<object> HandleAsync(LSP.TextDocumentPositionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var solution = requestContext.Context;

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<LSP.Location>();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var locations = await (s_findUsagesServiceLanguages.Contains(document.Project.Language)
                                                                        ? GetDefinitionsWithFindUsagesService(document, position, cancellationToken).ConfigureAwait(false)
                                                                        : GetDefinitionsWithDefinitionsService(document, position, cancellationToken).ConfigureAwait(false));

            // No definition found - see if we can get metadata as source but that's only applicable for C#\VB.
            if ((locations.Count == 0) && document.SupportsSemanticModel && this._metadataAsSourceService != null)
            {
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (symbol?.Locations.FirstOrDefault().IsInMetadata == true)
                {
                    var declarationFile = await this._metadataAsSourceService.GetGeneratedFileAsync(document.Project, symbol, false, cancellationToken).ConfigureAwait(false);

                    var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    locations.Add(new LSP.Location
                    {
                        Uri = new Uri(declarationFile.FilePath),
                        Range = ProtocolConversions.LinePositionToRange(linePosSpan)
                    });
                }
            }

            return locations.ToArray();
        }

        private async Task<List<LSP.Location>> GetDefinitionsWithDefinitionsService(Document document, int pos, CancellationToken cancellationToken)
        {
            var definitionService = document.Project.LanguageServices.GetService<IGoToDefinitionService>();

            var definitions = await definitionService.FindDefinitionsAsync(document, pos, cancellationToken).ConfigureAwait(false);
            var locations = new List<LSP.Location>();

            if (definitions != null && definitions.Count() > 0)
            {
                foreach (var definition in definitions)
                {
                    var definitionText = await definition.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    locations.Add(new LSP.Location
                    {
                        Uri = definition.Document.GetURI(),
                        Range = ProtocolConversions.TextSpanToRange(definition.SourceSpan, definitionText)
                    });
                }
            }

            return locations;
        }

        /// <summary>
        ///  Using the find usages service is more expensive than using the definitions service because a lot of unnecessary information is computed. However, some languages
        /// don't provide an <see cref="IGoToDefinitionService"/> implementation that will return definitions so we must use <see cref="IFindUsagesService"/>.
        /// </summary>
        private async Task<List<LSP.Location>> GetDefinitionsWithFindUsagesService(Document document, int pos, CancellationToken cancellationToken)
        {
            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();

            var context = new SimpleFindUsagesContext(cancellationToken);

            // Roslyn calls into third party extensions to compute reference results and needs to be on the UI thread to compute results.
            // This is not great for us and ideally we should ask for a Roslyn API where we can make this call without blocking the UI.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await findUsagesService.FindReferencesAsync(document, pos, context).ConfigureAwait(false);

            var locations = new List<LSP.Location>();

            var definitions = context.GetDefinitions();
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    foreach (var docSpan in definition.SourceSpans)
                    {
                        locations.Add(await ProtocolConversions.DocumentSpanToLocationAsync(docSpan, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            return locations;
        }
    }
}
