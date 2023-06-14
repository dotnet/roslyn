// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue;

[Export(typeof(IFeedbackDiagnosticFileProvider))]
internal sealed class EditAndContinueFeedbackDiagnosticFileProvider : IFeedbackDiagnosticFileProvider
{
    /// <summary>
    /// Name of the file displayed in VS Feedback UI.
    /// See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1714452.
    /// </summary>
    private const string ZipFileName = "source_files_and_binaries_updated_during_hot_reload.zip";

    private const string VSFeedbackSemaphoreDir = @"Microsoft\VSFeedbackCollector";
    private const string VSFeedbackSemaphoreFileName = "feedback.recording.json";

    /// <summary>
    /// VS Feedback creates a JSON file at the start of feedback session and deletes it when the session is over.
    /// Watching the file is currently the only way to detect the feedback session.
    /// </summary>
    private readonly string _vsFeedbackSemaphoreFullPath;
    private readonly FileSystemWatcher? _vsFeedbackSemaphoreFileWatcher;

    private readonly int _vsProcessId;
    private readonly DateTime _vsProcessStartTime;
    private readonly string _tempDir;

    private volatile int _isLogCollectionInProgress;

    private readonly Lazy<EditAndContinueLanguageService>? _encService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditAndContinueFeedbackDiagnosticFileProvider(
        [Import(AllowDefault = true)] Lazy<EditAndContinueLanguageService>? encService = null)
    {
        _encService = encService;

        var vsProcess = Process.GetCurrentProcess();

        _vsProcessId = vsProcess.Id;
        _vsProcessStartTime = vsProcess.StartTime;

        _tempDir = Path.GetTempPath();
        var vsFeedbackTempDir = Path.Combine(_tempDir, VSFeedbackSemaphoreDir);
        _vsFeedbackSemaphoreFullPath = Path.Combine(vsFeedbackTempDir, VSFeedbackSemaphoreFileName);

        // Directory may not exist in scenarios such as Razor integration tests
        if (!Directory.Exists(vsFeedbackTempDir))
        {
            return;
        }

        _vsFeedbackSemaphoreFileWatcher = new FileSystemWatcher(vsFeedbackTempDir, VSFeedbackSemaphoreFileName);
        _vsFeedbackSemaphoreFileWatcher.Created += (_, _) => OnFeedbackSemaphoreCreatedOrChanged();
        _vsFeedbackSemaphoreFileWatcher.Changed += (_, _) => OnFeedbackSemaphoreCreatedOrChanged();
        _vsFeedbackSemaphoreFileWatcher.Deleted += (_, _) => OnFeedbackSemaphoreDeleted();

        if (File.Exists(_vsFeedbackSemaphoreFullPath))
        {
            OnFeedbackSemaphoreCreatedOrChanged();
        }

        _vsFeedbackSemaphoreFileWatcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Reuse the same directory for multiple feedback sessions originating from the same VS instance.
    /// Log files for different debugging sessions will be in separate subdirectories so they will not collide,
    /// but the later feedback sessions will include all files logged for the previous sessions as well.
    /// Also if the compression and/or uploading of the zip file is not finished by the time the new recording starts
    /// we might not be able to write the new zip file to disk and the previous content might be uploaded instead.
    /// See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1716980
    /// </summary>
    private string GetLogDirectory()
        => Path.Combine(Path.Combine(_tempDir, $"EnC_{_vsProcessId}", "Log"));

    private string GetZipFilePath()
        => Path.Combine(Path.Combine(_tempDir, $"EnC_{_vsProcessId}", ZipFileName));

    public IReadOnlyCollection<string> GetFiles()
        => _vsFeedbackSemaphoreFileWatcher is null
           ? Array.Empty<string>()
           : (IReadOnlyCollection<string>)(new[] { GetZipFilePath() });

    private void OnFeedbackSemaphoreCreatedOrChanged()
    {
        if (!IsLoggingEnabledForCurrentVisualStudioInstance(_vsFeedbackSemaphoreFullPath))
        {
            // The semaphore file was created for another VS instance.
            return;
        }

        if (Interlocked.CompareExchange(ref _isLogCollectionInProgress, 1, 0) == 0)
        {
            _encService?.Value.SetFileLoggingDirectory(GetLogDirectory());
        }
    }

    private void OnFeedbackSemaphoreDeleted()
    {
        if (Interlocked.Exchange(ref _isLogCollectionInProgress, 0) == 1)
        {
            _encService?.Value.SetFileLoggingDirectory(logDirectory: null);

            // Including the zip files in VS Feedback is currently on best effort basis.
            // See https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1714439
            Task.Run(() =>
            {
                try
                {
                    ZipFile.CreateFromDirectory(GetLogDirectory(), GetZipFilePath());
                }
                catch
                {
                }
            });
        }
    }

    private bool IsLoggingEnabledForCurrentVisualStudioInstance(string semaphoreFilePath)
    {
        try
        {
            if (_vsProcessStartTime > File.GetCreationTime(semaphoreFilePath))
            {
                // Semaphore file is older than the running instance of VS
                return false;
            }

            // Check the contents of the semaphore file to see if it's for this instance of VS
            var content = File.ReadAllText(semaphoreFilePath);
            return JObject.Parse(content)["processIds"] is JContainer pidCollection && pidCollection.Values<int>().Contains(_vsProcessId);
        }
        catch
        {
            // Something went wrong opening or parsing the semaphore file - ignore it
            return false;
        }
    }
}
