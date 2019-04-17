// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentReferencesName)]
    internal class FindAllReferencesHandler : IRequestHandler<LSP.ReferenceParams, object[]>
    {
        public async Task<object[]> HandleRequestAsync(Solution solution, LSP.ReferenceParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var context = new SimpleFindUsagesContext(cancellationToken);

            await findUsagesService.FindReferencesAsync(document, position, context).ConfigureAwait(false);

            if (clientCapabilities?.HasVisualStudioLspCapability() == true)
            {
                return await GetReferenceGroupsAsync(request, context, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await GetLocationsAsync(request, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<LSP.ReferenceGroup[]> GetReferenceGroupsAsync(LSP.ReferenceParams request, SimpleFindUsagesContext context, CancellationToken cancellationToken)
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

            var referenceGroups = ArrayBuilder<LSP.ReferenceGroup>.GetInstance();
            foreach (var keyValuePair in definitionMap)
            {
                var definition = keyValuePair.Key;
                var references = keyValuePair.Value;

                var referenceGroup = new LSP.ReferenceGroup();
                var text = definition.GetClassifiedText();

                referenceGroup.Definition = await ProtocolConversions.DocumentSpanToLocationWithTextAsync(definition.SourceSpans.First(), text, cancellationToken).ConfigureAwait(false);
                referenceGroup.DefinitionIcon = new ImageElement(definition.Tags.GetFirstGlyph().GetImageId());

                var locationWithTexts = new List<LSP.LocationWithText>();
                foreach (var reference in references)
                {
                    var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(reference.SourceSpan, context.CancellationToken).ConfigureAwait(false);
                    var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                    var referenceLocation = await ProtocolConversions.DocumentSpanToLocationAsync(reference.SourceSpan, cancellationToken).ConfigureAwait(false);
                    var docText = await reference.SourceSpan.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
                    var classifiedText = new ClassifiedTextElement(classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
                    var locationWithText = new LSP.LocationWithText { Range = referenceLocation.Range, Uri = referenceLocation.Uri, Text = classifiedText };
                    locationWithTexts.Add(locationWithText);
                }

                referenceGroup.References = locationWithTexts.ToArray();
                referenceGroups.Add(referenceGroup);
            }

            return referenceGroups.ToArrayAndFree();
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
