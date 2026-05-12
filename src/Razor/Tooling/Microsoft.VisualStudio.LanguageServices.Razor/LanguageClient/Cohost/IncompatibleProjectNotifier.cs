// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using WorkspacesSR = Microsoft.CodeAnalysis.Razor.Workspaces.Resources.SR;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IProjectCapabilityListener))]
[Export(typeof(IIncompatibleProjectNotifier))]
[method: ImportingConstructor]
internal sealed class IncompatibleProjectNotifier(
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory) : IIncompatibleProjectNotifier, IProjectCapabilityListener
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<IncompatibleProjectNotifier>();

    private readonly HashSet<string> _frameworkProjects = new(PathUtilities.OSSpecificPathComparer);
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public void NotifyMissingDocument(Project project, string filePath)
    {
        // When this document was opened, we will have checked if it was a .NET Framework project, and we listened for that below.
        // Since this method is only called when we receive an LSP request for a document, and LSP only works on open documents,
        // we know that the capability check must have happened before this method was called, so our cache is as up to date as
        // possible for the specific file being asked about.
        lock (_frameworkProjects)
        {
            if (_frameworkProjects.Contains(project.FilePath.AssumeNotNull()))
            {
                // This project doesn't have the .NET Core C# capability, so it's a .NET Framework project and we don't want

                // to notify the user, as those projects use a different editor.
                return;
            }
        }

        _telemetryReporter.ReportEvent("cohost/missingDocument", Severity.Normal);
        _logger.Log(LogLevel.Error, $"{(
            project.AdditionalDocuments.Any(d => d.FilePath is not null && d.FilePath.IsRazorFilePath())
                ? WorkspacesSR.FormatIncompatibleProject_NotAnAdditionalFile(Path.GetFileName(filePath), project.Name)
                : WorkspacesSR.FormatIncompatibleProject_NoAdditionalFiles(Path.GetFileName(filePath), project.Name))}");
    }

    public void OnProjectCapabilityMatched(string projectFilePath, string capability, bool isMatch)
    {
        // We only track the .NET Core capability
        if (capability != WellKnownProjectCapabilities.DotNetCoreCSharp)
        {
            return;
        }

        lock (_frameworkProjects)
        {
            if (isMatch)
            {
                // The project is a .NET Core project, so we don't care, but just in case it used to be .NET Framework,
                // let's clean up.
                _frameworkProjects.Remove(projectFilePath);
            }
            else
            {
                // The project is not a .NET Core project, so add it to our list of framework projects.
                _frameworkProjects.Add(projectFilePath);
            }
        }
    }
}
