// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Feedback;

internal abstract class AbstractZippedLogFeedbackDiagnosticFileProvider : AbstractFeedbackDiagnosticFileProvider
{
    private readonly IVisualStudioFeedbackFileWatcherService _feedbackFileWatcherService;

    protected AbstractZippedLogFeedbackDiagnosticFileProvider(IVisualStudioFeedbackFileWatcherService feedbackFileWatcherService)
        : base(feedbackFileWatcherService)
    {
        _feedbackFileWatcherService = feedbackFileWatcherService;
    }

    protected abstract string LogDirectoryNamePrefix { get; }

    protected abstract string ZipFileName { get; }

    private string LogDirectory
        => Path.Combine(
            _feedbackFileWatcherService.TempDirectory,
            $"{LogDirectoryNamePrefix}_{_feedbackFileWatcherService.VisualStudioProcessId}",
            "Log");

    private string ZipFilePath
        => Path.Combine(
            _feedbackFileWatcherService.TempDirectory,
            $"{LogDirectoryNamePrefix}_{_feedbackFileWatcherService.VisualStudioProcessId}",
            ZipFileName);

    public override IReadOnlyCollection<string> GetFiles()
        => IsWatching ? [ZipFilePath] : [];

    protected override void OnFeedbackRecordingStarted()
        => SetLogDirectory(LogDirectory);

    protected override void OnFeedbackRecordingEnded()
    {
        SetLogDirectory(logDirectory: null);

        // Including zip files in VS Feedback is best effort.
        _ = Task.Run(() => CreateZipFile(LogDirectory, ZipFilePath));
    }

    protected abstract void SetLogDirectory(string? logDirectory);

    private static void CreateZipFile(string logDirectory, string zipFilePath)
    {
        try
        {
            ZipFile.CreateFromDirectory(logDirectory, zipFilePath);
        }
        catch
        {
            // ignore
        }
    }
}
