// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal abstract class AbstractGoToDefinitionHandler : ILspServiceDocumentRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]?>
{
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly IGlobalOptionService _globalOptions;

    public AbstractGoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
    {
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;

    public abstract Task<LSP.Location[]?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken);

    protected async Task<LSP.Location[]?> GetDefinitionAsync(LSP.TextDocumentPositionParams request, bool forSymbolType, RequestContext context, CancellationToken cancellationToken)
    {
        var workspace = context.Workspace;
        var document = context.Document;
        if (workspace is null || document is null)
            return null;

        var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);

        return await GetDefinitionsAsync(_globalOptions, _metadataAsSourceFileService, workspace, document, forSymbolType, linePosition, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<LSP.Location[]?> GetDefinitionsAsync(IGlobalOptionService globalOptions, IMetadataAsSourceFileService? metadataAsSourceFileService, Workspace workspace, Document document, bool forSymbolType, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var locations = ArrayBuilder<LSP.Location>.GetInstance();
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

        var service = document.GetLanguageService<INavigableItemsService>();
        if (service is null)
            return null;

        var definitions = await service.GetNavigableItemsAsync(document, position, forSymbolType, cancellationToken).ConfigureAwait(false);
        if (definitions.Length > 0)
        {
            foreach (var definition in definitions)
            {
                if (!ShouldInclude(definition, forSymbolType))
                    continue;

                var location = await ProtocolConversions.TextSpanToLocationAsync(
                    await definition.Document.GetRequiredDocumentAsync(document.Project.Solution, cancellationToken).ConfigureAwait(false),
                    definition.SourceSpan,
                    definition.IsStale,
                    cancellationToken).ConfigureAwait(false);
                locations.AddIfNotNull(location);
            }
        }
        else if (document.SupportsSemanticModel && metadataAsSourceFileService != null)
        {
            // No definition found - see if we can get metadata as source but that's only applicable for C#\VB.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, document.Project.Solution.Services, includeType: true, cancellationToken).ConfigureAwait(false);
            if (forSymbolType)
                symbol = symbol?.GetSymbolType();

            if (symbol != null && metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
            {
                var options = globalOptions.GetMetadataAsSourceOptions();
                var declarationFile = await metadataAsSourceFileService.GetGeneratedFileAsync(workspace, document.Project, symbol, signaturesOnly: false, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);

                var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                locations.Add(new LSP.Location
                {
                    DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(declarationFile.FilePath),
                    Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                });
            }
        }

        return locations.ToArrayAndFree();

        // local functions
        static bool ShouldInclude(INavigableItem item, bool forSymbolType)
        {
            if (item.Glyph is Glyph.Namespace)
            {
                // Never return namespace symbols as part of the go to definition result.
                return false;
            }

            if (!forSymbolType)
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
