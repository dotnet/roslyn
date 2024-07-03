// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LanguageServer.Handler.Diagnostics;

[ExportWorkspaceService(typeof(IDiagnosticProjectInformationService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioDiagnosticProjectInformationService() : IDiagnosticProjectInformationService
{
    public VSDiagnosticProjectInformation GetDiagnosticProjectInformation(Project project)
    {
        if (project.Solution.Workspace is VisualStudioWorkspace workspace)
        {
            var guid = workspace.GetProjectGuid(project.Id);
            if (guid != Guid.Empty)
            {
                return new()
                {
                    ProjectIdentifier = guid.ToString(),
                    ProjectName = project.Name,
                };
            }
        }

        return DefaultDiagnosticProjectInformationService.GetDiagnosticProjectInformationHelper(project);
    }
}
