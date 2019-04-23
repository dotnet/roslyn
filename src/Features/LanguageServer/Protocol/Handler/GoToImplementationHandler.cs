// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class GoToImplementationHandler
    {
        internal static async Task<LSP.Location[]> GotoImplementationAsync(Solution solution, LSP.TextDocumentPositionParams request, CancellationToken cancellationToken)
        {
            var locations = ArrayBuilder<LSP.Location>.GetInstance();

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return locations.ToArrayAndFree();
            }

            var findUsagesService = document.Project.LanguageServices.GetService<IFindUsagesService>();
            var position = await document.GetPositionFromLinePosition(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var context = new SimpleFindUsagesContext(cancellationToken);

            await findUsagesService.FindImplementationsAsync(document, position, context).ConfigureAwait(false);

            foreach (var sourceSpan in context.GetDefinitions().SelectMany(definition => definition.SourceSpans))
            {
                locations.Add(await ProtocolConversions.DocumentSpanToLocationAsync(sourceSpan, cancellationToken).ConfigureAwait(false));
            }

            return locations.ToArrayAndFree();
        }
    }
}
