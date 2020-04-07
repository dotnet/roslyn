// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspMethod(LSP.Methods.TextDocumentReferencesName), Shared]
    internal class FindAllReferencesHandler : IRequestHandler<LSP.ReferenceParams, LSP.VSReferenceItem[]>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandler(IThreadingContext threadingContext, IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _threadingContext = threadingContext;
            _metadataAsSourceFileService = metadataAsSourceFileService;
        }

        public async Task<LSP.VSReferenceItem[]> HandleRequestAsync(
            Solution solution,
            ReferenceParams referenceParams,
            ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken)
        {
            Debug.Assert(clientCapabilities.HasVisualStudioLspCapability());

            var document = solution.GetDocumentFromURI(referenceParams.TextDocument.Uri);
            if (document == null)
            {
                return Array.Empty<LSP.VSReferenceItem>();
            }

            var findUsagesService = document.GetRequiredLanguageService<IFindUsagesLSPService>();
            var position = await document.GetPositionFromLinePositionAsync(
                ProtocolConversions.PositionToLinePosition(referenceParams.Position), cancellationToken).ConfigureAwait(false);
            var context = new SimpleFindUsagesContext(cancellationToken);

            // Finds the references for the symbol at the specific position in the document, pushing the results to the context instance.
            await findUsagesService.FindReferencesAsync(document, position, context).ConfigureAwait(false);

            return await GetReferenceItemsAsync(document, position, context, cancellationToken).ConfigureAwait(false);
        }

        private async Task<LSP.VSReferenceItem[]> GetReferenceItemsAsync(
            Document document,
            int position,
            SimpleFindUsagesContext context,
            CancellationToken cancellationToken)
        {
            // Mapping each reference to its definition
            var definitionMap = new Dictionary<DefinitionItem, List<SourceReferenceItem>>();
            foreach (var reference in context.GetReferences())
            {
                if (!definitionMap.ContainsKey(reference.Definition))
                {
                    definitionMap.Add(reference.Definition, new List<SourceReferenceItem>());
                }

                definitionMap[reference.Definition].Add(reference);
            }

            // NOTE: Parts of FAR currently do not display correctly due to a bug in LSP.VSReferenceItem.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1088938/
            using var _ = ArrayBuilder<LSP.VSReferenceItem>.GetInstance(out var referenceItems);

            // Each reference is assigned its own unique id
            var id = 0;
            foreach (var (definition, references) in definitionMap)
            {
                // Creating the reference item that corresponds to the definition.
                var definitionItem = await GenerateReferenceItem(
                    id, definitionId: id, document, position, definition.SourceSpans.FirstOrDefault(), context, definition.DisplayableProperties,
                    definition.GetClassifiedText(), symbolUsageInfo: null, cancellationToken).ConfigureAwait(false);

                // If we have an empty location, skip this definition and its references.
                if (definitionItem.Location == null)
                {
                    continue;
                }

                referenceItems.Add(definitionItem);
                var definitionId = id;
                id++;

                // Creating a reference item for each reference.
                foreach (var reference in references)
                {
                    var referenceItem = await GenerateReferenceItem(
                        id, definitionId, document, position, reference.SourceSpan, context, reference.AdditionalProperties,
                        definitionText: null, reference.SymbolUsageInfo, cancellationToken).ConfigureAwait(false);

                    // If we have an empty location, skip this reference.
                    if (referenceItem.Location == null)
                    {
                        continue;
                    }

                    referenceItems.Add(referenceItem);
                    id++;
                };
            }

            return referenceItems.ToArray();

            async Task<LSP.VSReferenceItem> GenerateReferenceItem(
                int id,
                int? definitionId,
                Document originalDocument,
                int originalPosition,
                DocumentSpan documentSpan,
                SimpleFindUsagesContext context,
                ImmutableDictionary<string, string> properties,
                ClassifiedTextElement? definitionText,
                SymbolUsageInfo? symbolUsageInfo,
                CancellationToken cancellationToken)
            {
                LSP.Location? location = null;

                // If we have no source span, our location may be in metadata.
                if (documentSpan == default)
                {
                    var symbol = await SymbolFinder.FindSymbolAtPositionAsync(originalDocument, originalPosition, cancellationToken).ConfigureAwait(false);
                    if (symbol != null && symbol.Locations != null && !symbol.Locations.IsEmpty && symbol.Locations.First().IsInMetadata)
                    {
                        var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(
                            originalDocument.Project, symbol, allowDecompilation: false, cancellationToken).ConfigureAwait(false);

                        var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                        location = new LSP.Location
                        {
                            Uri = new Uri(declarationFile.FilePath),
                            Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                        };
                    }
                }
                else
                {
                    location = await ProtocolConversions.DocumentSpanToLocationAsync(documentSpan, cancellationToken).ConfigureAwait(false);
                }

                // TO-DO: The Origin property should be added once Rich-Nav is completed.
                // https://github.com/dotnet/roslyn/issues/42847
                var result = new LSP.VSReferenceItem
                {
                    ContainingMember = properties.TryGetValue(
                        AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var referenceContainingMember) ? referenceContainingMember : null,
                    ContainingType = properties.TryGetValue(
                        AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var referenceContainingType) ? referenceContainingType : null,
                    DefinitionId = definitionId,
                    DefinitionText = definitionText,    // Only definitions should have a non-null DefinitionText
                    DisplayPath = location?.Uri.LocalPath,
                    DocumentName = documentSpan == default ? null : documentSpan.Document.Name,
                    Id = id,
                    Kind = symbolUsageInfo.HasValue ? ProtocolConversions.SymbolUsageInfoToReferenceKinds(symbolUsageInfo.Value) : new ReferenceKind[] { },
                    Location = location,
                    ProjectName = documentSpan == default ? null : documentSpan.Document.Project.Name,
                    ResolutionStatus = ResolutionStatusKind.ConfirmedAsReference,
                };

                // Properly assigning the text property.
                if (id == definitionId)
                {
                    result.Text = definitionText;
                }
                else if (documentSpan != default)
                {
                    var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                        documentSpan, cancellationToken).ConfigureAwait(false);
                    var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                    var docText = await documentSpan.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    result.Text = new ClassifiedTextElement(
                        classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
                }

                return result;
            }
        }
    }
}
