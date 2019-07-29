// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentImplementationName)]
    internal class FindImplementationsHandler : IRequestHandler<LSP.TextDocumentPositionParams, object>
    {
        public async Task<object> HandleRequestAsync(Solution solution, LSP.TextDocumentPositionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(keepThreadContext);

            var context = new SimpleFindUsagesContext(cancellationToken);

            await findUsagesService.FindImplementationsAsync(document, position, context).ConfigureAwait(keepThreadContext);

            foreach (var definition in context.GetDefinitions())
            {
                var text = definition.GetClassifiedText();
                foreach (var sourceSpan in context.GetDefinitions().SelectMany(definition => definition.SourceSpans))
                {
                    if (clientCapabilities?.HasVisualStudioLspCapability() == true)
                    {
                        locations.Add(await ProtocolConversions.DocumentSpanToLocationWithTextAsync(sourceSpan, text, cancellationToken).ConfigureAwait(keepThreadContext));
                    }
                    else
                    {
                        locations.Add(await ProtocolConversions.DocumentSpanToLocationAsync(sourceSpan, cancellationToken).ConfigureAwait(keepThreadContext));
                    }
                }
            }

            return locations.ToArrayAndFree();
        }
    }
}
