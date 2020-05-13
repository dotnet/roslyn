// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(MSLSPMethods.ProjectContextsName)]
    internal class GetTextDocumentWithContextHandler : IRequestHandler<GetTextDocumentWithContextParams, ActiveProjectContexts?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GetTextDocumentWithContextHandler()
        {
        }

        public Task<ActiveProjectContexts?> HandleRequestAsync(
            Solution solution,
            GetTextDocumentWithContextParams request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            var startingDocument = solution.GetDocumentFromURI(request.TextDocument.Uri, clientName);

            if (startingDocument == null || startingDocument.FilePath == null)
            {
                return Task.FromResult<ActiveProjectContexts?>(null);
            }

            var allDocumentIds = solution.GetDocumentIdsWithFilePath(startingDocument.FilePath);
            var contexts = new List<ProjectContext>();

            foreach (var documentId in allDocumentIds)
            {
                var project = solution.GetRequiredProject(documentId.ProjectId);
                var context = new ProjectContext
                {
                    Id = ProtocolConversions.ProjectIdToProjectContextId(project.Id),
                    Label = project.Name
                };

                if (project.Language == LanguageNames.CSharp)
                {
                    context.Kind = ProjectContextKind.CSharp;
                }
                else if (project.Language == LanguageNames.VisualBasic)
                {
                    context.Kind = ProjectContextKind.VisualBasic;
                }

                contexts.Add(context);
            }

            // If the document is open, it doesn't matter which DocumentId we pass to GetDocumentIdInCurrentContext since
            // all the documents are linked at that point, so we can just pass the first arbitrarily. If the document is closed
            // GetDocumentIdInCurrentContext will just return the same ID back, which means we're going to pick the first
            // ID in GetDocumentIdsWithFilePath, but there's really nothing we can do since we don't have contexts for
            // close documents anyways.
            var currentContextDocumentId = solution.Workspace.GetDocumentIdInCurrentContext(allDocumentIds.First());

            return Task.FromResult<ActiveProjectContexts?>(new ActiveProjectContexts
            {
                ProjectContexts = contexts.ToArray(),
                DefaultIndex = allDocumentIds.IndexOf(currentContextDocumentId)
            });
        }
    }
}
