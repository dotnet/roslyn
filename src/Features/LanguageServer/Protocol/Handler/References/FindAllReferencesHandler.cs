// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    // The VS LSP client supports streaming using IProgress<T> on various requests.
    // However, this is not yet supported through Live Share, so deserialization fails on the IProgress<T> property.
    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1043376 tracks Live Share support for this (committed for 16.6).
    [ExportLspMethod(LSP.Methods.TextDocumentReferencesName), Shared]
    internal class FindAllReferencesHandler : IRequestHandler<LSP.ReferenceParams, object[]>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandler(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task<object[]> HandleRequestAsync(Solution solution, ReferenceParams referenceParams, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            // The VS LSP client supports streaming using IProgress<T> on various requests.
            // However, this is not yet supported through Live Share, so deserialization fails on the IProgress<T> property.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1043376 tracks Live Share support for this (committed for 16.6).
            var locations = ArrayBuilder<LSP.Location>.GetInstance();
            var document = solution.GetDocumentFromURI(referenceParams.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesLSPService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(referenceParams.Position), cancellationToken).ConfigureAwait(false);
            var context = new SimpleFindUsagesContext(cancellationToken);

            await findUsagesService.FindReferencesAsync(document, position, context).ConfigureAwait(false);

            if (clientCapabilities?.HasVisualStudioLspCapability() == true)
            {
                return await GetReferenceItemsAsync(referenceParams, context, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await GetLocationsAsync(referenceParams, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<LSP.VSReferenceItem[]> GetReferenceItemsAsync(LSP.ReferenceParams request, SimpleFindUsagesContext context, CancellationToken cancellationToken)
        {
            var definitionMap = new Dictionary<DefinitionItem, List<SourceReferenceItem>>();
            foreach (var reference in context.GetReferences())
            {
                if (!definitionMap.ContainsKey(reference.Definition))
                {
                    definitionMap.Add(reference.Definition, new List<SourceReferenceItem>());
                }

                definitionMap[reference.Definition].Add(reference);
            }

            // Parts of FAR currently do not display correctly due to a bug in VSReferenceItem on the LSP side.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1088938/
            var referenceItems = ArrayBuilder<LSP.VSReferenceItem>.GetInstance();

            // Each reference is assigned its own unique id
            var id = 0;
            foreach (var definitionAndReferencesPair in definitionMap)
            {
                // Creating the reference item that corresponds to the definition
                var definition = definitionAndReferencesPair.Key;
                var definitionItem = await GenerateReferenceItem(id, definitionId: id, definition.SourceSpans.FirstOrDefault(), context, definition.DisplayableProperties,
                    cancellationToken, definition.GetClassifiedText()).ConfigureAwait(false);
                referenceItems.Add(definitionItem);

                var definitionId = id;
                id++;

                // Creating a reference item for each reference
                var references = definitionAndReferencesPair.Value;
                foreach (var reference in references)
                {
                    var referenceItem = await GenerateReferenceItem(id, definitionId, reference.SourceSpan, context, reference.AdditionalProperties, cancellationToken).ConfigureAwait(false);
                    referenceItems.Add(referenceItem);
                    id++;
                };
            }

            return referenceItems.ToArrayAndFree();

            static async Task<VSReferenceItem> GenerateReferenceItem(
                int id,
                int? definitionId,
                DocumentSpan documentSpan,
                SimpleFindUsagesContext context,
                ImmutableDictionary<string, string> properties,
                CancellationToken cancellationToken,
                ClassifiedTextElement definitionText = null)
            {
                var location = await ProtocolConversions.DocumentSpanToLocationAsync(documentSpan, cancellationToken).ConfigureAwait(false);
                var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, context.CancellationToken).ConfigureAwait(false);
                var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                var docText = await documentSpan.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

                return new LSP.VSReferenceItem
                {
                    ContainingMember = properties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var referenceContainingMember) ? referenceContainingMember : null,
                    ContainingType = properties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var referenceContainingType) ? referenceContainingType : null,
                    DefinitionId = definitionId,
                    DefinitionText = definitionText,    // Only definitions should have a non-null DefinitionText
                    DocumentName = documentSpan.Document.Name,
                    Id = id,
                    Location = new LSP.Location { Range = location.Range, Uri = location.Uri },
                    ProjectName = documentSpan.Document.Project.Name,
                    ResolutionStatus = ResolutionStatusKind.ConfirmedAsReference,
                    Text = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan)))),
                };
            }
        }

        private static async Task<LSP.Location[]> GetLocationsAsync(LSP.ReferenceParams request, SimpleFindUsagesContext context, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            if (request.Context.IncludeDeclaration)
            {
                foreach (var definition in context.GetDefinitions())
                {
                    foreach (var docSpan in definition.SourceSpans)
                    {
                        locations.Add(await ProtocolConversions.DocumentSpanToLocationAsync(docSpan, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            foreach (var reference in context.GetReferences())
            {
                locations.Add(await ProtocolConversions.DocumentSpanToLocationAsync(reference.SourceSpan, cancellationToken).ConfigureAwait(false));
            }

            return locations.ToArrayAndFree();
        }
    }
}
