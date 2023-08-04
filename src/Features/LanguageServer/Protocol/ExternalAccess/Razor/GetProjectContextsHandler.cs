// Licensed to the .NET Foundation under one or more agreements.
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
using System.IO;

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

        var contextList = ProjectContextHelper.GetContextList(context.Workspace, context.Solution, request.TextDocument.Uri);
        if (contextList is null)
        {
            return SpecializedTasks.Null<ProjectContextList>();
        }

        var idToIntermediateOutputMap = new Dictionary<string, string?>();
        foreach (var projectContext in contextList.ProjectContexts)
        {
            if (projectContext is null)
            {
                continue;
            }

            var projectId = ProtocolConversions.ProjectContextToProjectId(projectContext);
            var project = context.Solution.GetRequiredProject(projectId);

            var dllOutputPath = project.CompilationOutputInfo.AssemblyPath;
            Contract.ThrowIfNull(dllOutputPath);

            var outputDirectory = Directory.GetParent(dllOutputPath)!.FullName;
            idToIntermediateOutputMap[projectContext.Id] = outputDirectory;
        }

        return Task.FromResult<ProjectContextList?>(new ProjectContextList
        {
            ProjectContexts = contextList,
            ProjectIdToIntermediatePathMap = idToIntermediateOutputMap
        });
    }
}
