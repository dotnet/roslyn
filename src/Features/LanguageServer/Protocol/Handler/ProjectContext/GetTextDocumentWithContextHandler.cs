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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(MSLSPMethods.ProjectContextsName)]
    internal class GetTextDocumentWithContextHandler : AbstractRequestHandler<GetTextDocumentWithContextParams, ActiveProjectContexts?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GetTextDocumentWithContextHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override Task<ActiveProjectContexts?> HandleRequestAsync(GetTextDocumentWithContextParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var documents = SolutionProvider.GetDocuments(request.TextDocument.Uri, context.ClientName);

            if (!documents.Any())
            {
                return SpecializedTasks.Null<ActiveProjectContexts>();
            }

            var contexts = new List<ProjectContext>();

            foreach (var document in documents)
            {
                var project = document.Project;
                var projectContext = new ProjectContext
                {
                    Id = ProtocolConversions.ProjectIdToProjectContextId(project.Id),
                    Label = project.Name
                };

                if (project.Language == LanguageNames.CSharp)
                {
                    projectContext.Kind = ProjectContextKind.CSharp;
                }
                else if (project.Language == LanguageNames.VisualBasic)
                {
                    projectContext.Kind = ProjectContextKind.VisualBasic;
                }

                contexts.Add(projectContext);
            }

            // If the document is open, it doesn't matter which DocumentId we pass to GetDocumentIdInCurrentContext since
            // all the documents are linked at that point, so we can just pass the first arbitrarily. If the document is closed
            // GetDocumentIdInCurrentContext will just return the same ID back, which means we're going to pick the first
            // ID in GetDocumentIdsWithFilePath, but there's really nothing we can do since we don't have contexts for
            // close documents anyways.
            var openDocument = documents.First();
            var currentContextDocumentId = openDocument.Project.Solution.Workspace.GetDocumentIdInCurrentContext(openDocument.Id);

            return Task.FromResult<ActiveProjectContexts?>(new ActiveProjectContexts
            {
                ProjectContexts = contexts.ToArray(),
                DefaultIndex = documents.IndexOf(d => d.Id == currentContextDocumentId)
            });
        }
    }
}
