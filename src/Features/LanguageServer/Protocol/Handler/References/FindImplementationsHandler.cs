﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentImplementationName, mutatesSolutionState: false)]
    internal class FindImplementationsHandler : IRequestHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindImplementationsHandler()
        {
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;

        public async Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = context.Document;
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetRequiredService<IFindUsagesService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var findUsagesContext = new SimpleFindUsagesContext(cancellationToken);

            await FindImplementationsAsync(findUsagesService, document, position, findUsagesContext).ConfigureAwait(false);

            foreach (var definition in findUsagesContext.GetDefinitions())
            {
                var text = definition.GetClassifiedText();
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    if (context.ClientCapabilities?.HasVisualStudioLspCapability() == true)
                    {
                        locations.AddIfNotNull(await ProtocolConversions.DocumentSpanToLocationWithTextAsync(sourceSpan, text, cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        locations.AddIfNotNull(await ProtocolConversions.DocumentSpanToLocationAsync(sourceSpan, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            return locations.ToArrayAndFree();
        }

        protected virtual Task FindImplementationsAsync(IFindUsagesService findUsagesService, Document document, int position, SimpleFindUsagesContext context)
            => findUsagesService.FindImplementationsAsync(document, position, context);
    }
}
