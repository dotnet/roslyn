// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractGoToDefinitionHandler<RequestType, ResponseType> : AbstractRequestHandler<RequestType, ResponseType>
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        public AbstractGoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, ILspSolutionProvider solutionProvider) : base(solutionProvider)
            => _metadataAsSourceFileService = metadataAsSourceFileService;

        protected async Task<LSP.Location[]> GetDefinitionAsync(LSP.TextDocumentPositionParams request, bool typeOnly, string? clientName, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = SolutionProvider.GetDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var definitionService = document.Project.LanguageServices.GetRequiredService<IGoToDefinitionService>();
            var definitions = await definitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (definitions != null && definitions.Count() > 0)
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
                if (symbol != null && symbol.Locations != null && !symbol.Locations.IsEmpty && symbol.Locations.First().IsInMetadata)
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
