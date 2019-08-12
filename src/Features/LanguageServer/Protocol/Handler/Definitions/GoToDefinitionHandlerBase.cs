// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class GoToDefinitionHandlerBase
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        public GoToDefinitionHandlerBase(IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
        }

        protected async Task<LSP.Location[]> GetDefinitionAsync(Solution solution, LSP.TextDocumentPositionParams request, bool typeOnly, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var definitionService = document.Project.LanguageServices.GetService<IGoToDefinitionService>();
            var definitions = await definitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (definitions != null && definitions.Count() > 0)
            {
                foreach (var definition in definitions)
                {
                    if (!ShouldInclude(definition, typeOnly))
                    {
                        continue;
                    }

                    var definitionText = await definition.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    locations.Add(new LSP.Location
                    {
                        Uri = definition.Document.GetURI(),
                        Range = ProtocolConversions.TextSpanToRange(definition.SourceSpan, definitionText),
                    });
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
