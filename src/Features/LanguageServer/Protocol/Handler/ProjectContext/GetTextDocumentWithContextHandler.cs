// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(GetTextDocumentWithContextHandler)), Shared]
    [Method(VSMethods.GetProjectContextsName)]
    internal class GetTextDocumentWithContextHandler : ILspServiceDocumentRequestHandler<VSGetProjectContextsParams, VSProjectContextList?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GetTextDocumentWithContextHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(VSGetProjectContextsParams request) => new TextDocumentIdentifier { Uri = request.TextDocument.Uri };

        public Task<VSProjectContextList?> HandleRequestAsync(VSGetProjectContextsParams request, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Workspace);
            Contract.ThrowIfNull(context.Solution);

            // We specifically don't use context.Document here because we want multiple. We also don't need
            // all of the document info, just the Id is enough
            var documentIds = context.Solution.GetDocumentIds(request.TextDocument.Uri);

            if (!documentIds.Any())
            {
                return SpecializedTasks.Null<VSProjectContextList>();
            }

            var contexts = new List<VSProjectContext>();

            foreach (var documentId in documentIds)
            {
                var project = context.Solution.GetRequiredProject(documentId.ProjectId);
                var projectContext = ProtocolConversions.ProjectToProjectContext(project);
                contexts.Add(projectContext);
            }

            // If the document is open, it doesn't matter which DocumentId we pass to GetDocumentIdInCurrentContext since
            // all the documents are linked at that point, so we can just pass the first arbitrarily. If the document is closed
            // GetDocumentIdInCurrentContext will just return the same ID back, which means we're going to pick the first
            // ID in GetDocumentIdsWithFilePath, but there's really nothing we can do since we don't have contexts for
            // close documents anyways.
            var openDocumentId = documentIds.First();
            var currentContextDocumentId = context.Workspace.GetDocumentIdInCurrentContext(openDocumentId);

            return Task.FromResult<VSProjectContextList?>(new VSProjectContextList
            {
                ProjectContexts = contexts.ToArray(),
                DefaultIndex = documentIds.IndexOf(d => d == currentContextDocumentId)
            });
        }
    }
}
