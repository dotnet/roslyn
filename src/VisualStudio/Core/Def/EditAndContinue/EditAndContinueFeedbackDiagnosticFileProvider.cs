// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;
using Microsoft.VisualStudio.LanguageServices.Feedback;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue;

[Export(typeof(IFeedbackDiagnosticFileProvider))]
internal sealed class EditAndContinueFeedbackDiagnosticFileProvider : AbstractZippedLogFeedbackDiagnosticFileProvider
{
    private readonly Lazy<IHostWorkspaceProvider> _workspaceProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditAndContinueFeedbackDiagnosticFileProvider(
        Lazy<IHostWorkspaceProvider> workspaceProvider,
        IVisualStudioFeedbackFileWatcherService feedbackFileWatcherService)
        : base(feedbackFileWatcherService)
    {
        _workspaceProvider = workspaceProvider;

        StartListeningToFeedbackRecording();
    }

    /// <summary>
    /// Reuse the same directory for multiple feedback sessions originating from the same VS instance.
    /// Log files for different debugging sessions will be in separate subdirectories so they will not collide,
    /// but the later feedback sessions will include all files logged for the previous sessions as well.
    /// Also if the compression and/or uploading of the zip file is not finished by the time the new recording starts
    /// we might not be able to write the new zip file to disk and the previous content might be uploaded instead.
    /// See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1716980
    /// </summary>
    protected override string LogDirectoryNamePrefix => "EnC";

    /// <summary>
    /// Name of the file displayed in VS Feedback UI.
    /// See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1714452.
    /// </summary>
    protected override string ZipFileName => "source_files_and_binaries_updated_during_hot_reload.zip";

    protected override void SetLogDirectory(string? logDirectory)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var proxy = new RemoteEditAndContinueServiceProxy(_workspaceProvider.Value.Workspace.Services.SolutionServices);
                await proxy.SetFileLoggingDirectoryAsync(logDirectory, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        });
    }
}
