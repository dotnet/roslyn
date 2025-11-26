// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(GetTextDocumentWithContextHandler)), Shared]
[Method(VSMethods.GetProjectContextsName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class GetTextDocumentWithContextHandler() : ILspServiceDocumentRequestHandler<VSGetProjectContextsParams, VSProjectContextList?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(VSGetProjectContextsParams request) => new() { DocumentUri = request.TextDocument.DocumentUri };

    public async Task<VSProjectContextList?> HandleRequestAsync(VSGetProjectContextsParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Workspace);
        Contract.ThrowIfNull(context.Solution);

        // We specifically don't use context.Document here because we want multiple. We also don't need
        // all of the document info, just the Id is enough
        var documentIds = context.Solution.GetDocumentIds(request.TextDocument.DocumentUri);

        if (!documentIds.Any())
        {
            return null;
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

        // Create a key that uniquely identifies this set of contexts. We use this to track the user's preferred context
        // on the client side.
        var keyString = string.Join(";", contexts.Select(c => c.Label).OrderBy(l => l));
        var keyHash = XxHash128.Hash(Encoding.Unicode.GetBytes(keyString));
        var key = Convert.ToBase64String(keyHash);

        return new VSProjectContextList
        {
            Key = key,
            ProjectContexts = [.. contexts],
            DefaultIndex = documentIds.IndexOf(d => d == currentContextDocumentId)
        };
    }
}
