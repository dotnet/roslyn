// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;
using Microsoft.VisualStudio.LanguageServices.Feedback;

namespace Microsoft.VisualStudio.Razor.Feedback;

[Export(typeof(IFeedbackDiagnosticFileProvider))]
internal sealed class RazorFormattingFeedbackDiagnosticFileProvider : AbstractZippedLogFeedbackDiagnosticFileProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker;

    [ImportingConstructor]
    public RazorFormattingFeedbackDiagnosticFileProvider(
        IRemoteServiceInvoker remoteServiceInvoker,
        IVisualStudioFeedbackFileWatcherService feedbackFileWatcherService)
        : base(feedbackFileWatcherService)
    {
        _remoteServiceInvoker = remoteServiceInvoker;

        StartListeningToFeedbackRecording();
    }

    protected override string LogDirectoryNamePrefix => "RazorFormatting";

    protected override string ZipFileName => "RazorFormattingLogs.zip";

    protected override void SetLogDirectory(string? logDirectory)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _remoteServiceInvoker
                    .TryInvokeAsync<IRemoteFormattingService, bool>(
                        (service, cancellationToken) => service.SetFormattingLogDirectoryAsync(logDirectory, cancellationToken),
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        });
    }
}
