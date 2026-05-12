// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Export(typeof(IIncompatibleProjectNotifier))]
[method: ImportingConstructor]
internal sealed class IncompatibleProjectNotifier(
     ITelemetryReporter telemetryReporter,
     ILoggerFactory loggerFactory) : IIncompatibleProjectNotifier
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<IncompatibleProjectNotifier>();
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public void NotifyMissingDocument(Project project, string filePath)
    {
        _telemetryReporter.ReportEvent("cohost/missingDocument", Severity.Normal);
        _logger.Log(LogLevel.Error, $"{(
            project.AdditionalDocuments.Any(d => d.FilePath is not null && d.FilePath.IsRazorFilePath())
                ? WorkspacesSR.FormatIncompatibleProject_NotAnAdditionalFile(Path.GetFileName(filePath), project.Name)
                : WorkspacesSR.FormatIncompatibleProject_NoAdditionalFiles(Path.GetFileName(filePath), project.Name))}");
    }
}
