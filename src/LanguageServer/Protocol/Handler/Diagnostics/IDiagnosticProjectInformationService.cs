// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal interface IDiagnosticProjectInformationService : IWorkspaceService
{
    /// <summary>
    /// Allows the workspace to customize the project information it returns for a particular project.
    /// </summary>
    VSDiagnosticProjectInformation GetDiagnosticProjectInformation(Project project);
}

[ExportWorkspaceService(typeof(IDiagnosticProjectInformationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultDiagnosticProjectInformationService() : IDiagnosticProjectInformationService
{
    public VSDiagnosticProjectInformation GetDiagnosticProjectInformation(Project project)
        => GetDiagnosticProjectInformationHelper(project);

    public static VSDiagnosticProjectInformation GetDiagnosticProjectInformationHelper(Project project)
        => new()
        {
            // When we have nothing better, just return the underlying roslyn-internal guid of this project. In
            // practice, in VS this will actually become the real VS project guid for this project, as long as we can
            // find that.
            ProjectIdentifier = project.Id.Id.ToString(),
            ProjectName = project.Name,
        };
}
