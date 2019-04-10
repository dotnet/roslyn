// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class FindAllReferencesHandler
    {
        public static async Task<LSP.Location[]> FindAllReferencesAsync(Solution solution, LSP.ReferenceParams request, CancellationToken cancellationToken)
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

            await findUsagesService.FindReferencesAsync(document, position, context).ConfigureAwait(false);

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
