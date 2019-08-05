// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class FindAllReferencesHandler : ILspRequestHandler<LSP.ReferenceParams, object[], Solution>
    {
        private readonly IThreadingContext _threadingContext;

        public FindAllReferencesHandler(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task<object[]> HandleAsync(LSP.ReferenceParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();
            var solution = requestContext.Context;
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var context = new SimpleFindUsagesContext(cancellationToken);

            // Roslyn calls into third party extensions to compute reference results and needs to be on the UI thread to compute results.
            // This is not great for us and ideally we should ask for a Roslyn API where we can make this call without blocking the UI.
            if (VsTaskLibraryHelper.ServiceInstance != null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }

            await findUsagesService.FindReferencesAsync(document, position, context).ConfigureAwait(false);

            if (requestContext?.ClientCapabilities?.ToObject<VSClientCapabilities>()?.HasVisualStudioLspCapability() == true)
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

                var locationWithTexts = new ArrayBuilder<LSP.LocationWithText>();
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

                referenceGroup.References = locationWithTexts.ToArrayAndFree();
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
