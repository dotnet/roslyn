﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(GetProjectContextsHandler)), Shared]
[Method(GetProjectContextsName)]
internal class GetProjectContextsHandler : ILspServiceDocumentRequestHandler<ProjectContextsParams, ProjectContextList?>
{
    private const string GetProjectContextsName = "roslyn/getProjectContexts";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public GetProjectContextsHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(ProjectContextsParams request)
        => request.TextDocument;

    public Task<ProjectContextList?> HandleRequestAsync(ProjectContextsParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Workspace);
        Contract.ThrowIfNull(context.Solution);

        // We specifically don't use context.Document here because we want multiple
        var documents = context.Solution.GetDocuments(request.TextDocument.Uri);

        if (!documents.Any())
        {
            return SpecializedTasks.Null<ProjectContextList>();
        }

        var idToIntermediateOutputMap = new Dictionary<string, string?>();
        var contexts = new List<VSProjectContext>();

        foreach (var document in documents)
        {
            var project = document.Project;
            var projectContext = ProtocolConversions.ProjectToProjectContext(project);
            contexts.Add(projectContext);
            idToIntermediateOutputMap[projectContext.Id] = project.CompilationOutputInfo.AssemblyPath;
        }

        // If the document is open, it doesn't matter which DocumentId we pass to GetDocumentIdInCurrentContext since
        // all the documents are linked at that point, so we can just pass the first arbitrarily. If the document is closed
        // GetDocumentIdInCurrentContext will just return the same ID back, which means we're going to pick the first
        // ID in GetDocumentIdsWithFilePath, but there's really nothing we can do since we don't have contexts for
        // close documents anyways.
        var openDocument = documents.First();
        var currentContextDocumentId = context.Workspace.GetDocumentIdInCurrentContext(openDocument.Id);

        return Task.FromResult<ProjectContextList?>(new ProjectContextList
        {
            ProjectContexts = contexts.ToArray(),
            DefaultIndex = documents.IndexOf(d => d.Id == currentContextDocumentId),
            ProjectIdToIntermediatePathMap = idToIntermediateOutputMap
        });
    }
}
