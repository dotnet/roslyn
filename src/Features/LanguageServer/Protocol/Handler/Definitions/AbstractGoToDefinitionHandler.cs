// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractGoToDefinitionHandler : IRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        public AbstractGoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService)
            => _metadataAsSourceFileService = metadataAsSourceFileService;

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;
        public abstract Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken);

        protected async Task<LSP.Location[]> GetDefinitionAsync(LSP.TextDocumentPositionParams request, bool typeOnly, RequestContext context, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = context.Document;
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var definitions = await GetDefinitions(document, position, cancellationToken).ConfigureAwait(false);
            if (definitions?.Any() == true)
            {
                foreach (var definition in definitions)
                {
                    if (!ShouldInclude(definition, typeOnly))
                    {
                        continue;
                    }

                    var location = await ProtocolConversions.TextSpanToLocationAsync(definition.Document, definition.SourceSpan, cancellationToken).ConfigureAwait(false);
                    locations.AddIfNotNull(location);
                }
            }
            else if (document.SupportsSemanticModel && _metadataAsSourceFileService != null)
            {
                // No definition found - see if we can get metadata as source but that's only applicable for C#\VB.
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (symbol != null && !symbol.Locations.IsEmpty && symbol.Locations.First().IsInMetadata)
                {
                    if (!typeOnly || symbol is ITypeSymbol)
                    {
                        var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(document.Project, symbol, false, cancellationToken).ConfigureAwait(false);

                        var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                        locations.Add(new LSP.Location
                        {
                            Uri = new Uri(declarationFile.FilePath),
                            Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                        });
                    }
                }
            }

            return locations.ToArrayAndFree();

            // local functions
            static bool ShouldInclude(INavigableItem item, bool typeOnly)
            {
                if (item.Glyph is Glyph.Namespace)
                {
                    // Never return namespace symbols as part of the go to definition result.
                    return false;
                }

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

            static async Task<IEnumerable<INavigableItem>?> GetDefinitions(Document document, int position, CancellationToken cancellationToken)
            {
                // Try IFindDefinitionService first. Until partners implement this, it could fail to find a service, so fall back if it's null.
                var findDefinitionService = document.GetLanguageService<IFindDefinitionService>();
                if (findDefinitionService != null)
                {
                    return await findDefinitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
                }

                // Removal of this codepath is tracked by https://github.com/dotnet/roslyn/issues/50391.
                var goToDefinitionsService = document.GetRequiredLanguageService<IGoToDefinitionService>();
                return await goToDefinitionsService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
