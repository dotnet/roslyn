// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal enum LogMessageSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Implements EnC logging.
/// 
/// Writes log messages to:
/// - fixed size rolling tracing log captured in a memory dump,
/// - a file log, if a log directory is provided,
/// - log service, if avaiable.
/// </summary>
internal sealed class TraceLog(string name, IEditAndContinueLogReporter? logService = null, int logSize = 2048)
{
    internal sealed class FileLogger(string logDirectory, TraceLog traceLog)
    {
        private readonly string _logDirectory = logDirectory;
        private readonly TraceLog _traceLog = traceLog;

        public void Append(string entry)
        {
            string? path = null;

            try
            {
                path = Path.Combine(_logDirectory, _traceLog._name + ".log");
                File.AppendAllLines(path, [entry]);
            }
            catch (Exception e)
            {
                _traceLog.AppendFileLoggingErrorInMemory(path, e);
            }
        }

        private string CreateSessionDirectory(DebuggingSessionId sessionId, string relativePath)
        {
            Contract.ThrowIfNull(_logDirectory);
            var directory = Path.Combine(_logDirectory, sessionId.Ordinal.ToString(), relativePath);
            Directory.CreateDirectory(directory);
            return directory;
        }

        private string MakeSourceFileLogPath(Document document, string suffix, UpdateId updateId, int? generation)
        {
            Debug.Assert(document.FilePath != null);
            Debug.Assert(document.Project.FilePath != null);

            var projectDir = PathUtilities.GetDirectoryName(document.Project.FilePath)!;
            var documentDir = PathUtilities.GetDirectoryName(document.FilePath)!;
            var extension = PathUtilities.GetExtension(document.FilePath);
            var fileName = PathUtilities.GetFileName(document.FilePath, includeExtension: false);

            var relativeDir = PathUtilities.IsSameDirectoryOrChildOf(documentDir, projectDir) ? PathUtilities.GetRelativePath(projectDir, documentDir) : documentDir;
            relativeDir = relativeDir.Replace('\\', '_').Replace('/', '_');

            var directory = CreateSessionDirectory(updateId.SessionId, Path.Combine(document.Project.Name, relativeDir));
            return Path.Combine(directory, $"{fileName}.{updateId.Ordinal}.{generation?.ToString() ?? "-"}.{suffix}{extension}");
        }

        public void Write(DebuggingSessionId sessionId, ImmutableArray<byte> bytes, string directory, string fileName)
        {
            string? path = null;
            try
            {
                path = Path.Combine(CreateSessionDirectory(sessionId, directory), fileName);
                File.WriteAllBytes(path, [.. bytes]);
            }
            catch (Exception e)
            {
                _traceLog.AppendFileLoggingErrorInMemory(path, e);
            }
        }

        public async ValueTask WriteAsync(Func<Stream, CancellationToken, ValueTask> writer, DebuggingSessionId sessionId, string directory, string fileName, CancellationToken cancellationToken)
        {
            string? path = null;
            try
            {
                path = Path.Combine(CreateSessionDirectory(sessionId, directory), fileName);
                using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write | FileShare.Delete);
                await writer(file, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _traceLog.AppendFileLoggingErrorInMemory(path, e);
            }
        }

        public async ValueTask WriteDocumentAsync(Document document, string fileNameSuffix, UpdateId updateId, int? generation, CancellationToken cancellationToken)
        {
            Debug.Assert(document.FilePath != null);

            string? path = null;
            try
            {
                path = MakeSourceFileLogPath(document, fileNameSuffix, updateId, generation);
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write | FileShare.Delete);
                using var writer = new StreamWriter(file, text.Encoding ?? Encoding.UTF8);
                text.Write(writer, cancellationToken);
            }
            catch (Exception e)
            {
                _traceLog.AppendFileLoggingErrorInMemory(path, e);
            }
        }

        public async ValueTask WriteDocumentChangeAsync(Document? oldDocument, Document? newDocument, UpdateId updateId, int? generation, CancellationToken cancellationToken)
        {
            if (oldDocument?.FilePath != null)
            {
                await WriteDocumentAsync(oldDocument, fileNameSuffix: "old", updateId, generation, cancellationToken).ConfigureAwait(false);
            }

            if (newDocument?.FilePath != null)
            {
                await WriteDocumentAsync(newDocument, fileNameSuffix: "new", updateId, generation, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private readonly string[] _log = new string[logSize];
    private readonly string _name = name;
    private int _currentLine;

    public FileLogger? FileLog { get; private set; }

    public void SetLogDirectory(string? logDirectory)
    {
        FileLog = (logDirectory != null) ? new FileLogger(logDirectory, this) : null;
    }

    private void AppendInMemory(string entry)
    {
        var index = Interlocked.Increment(ref _currentLine);
        _log[(index - 1) % _log.Length] = entry;
    }

    private void AppendFileLoggingErrorInMemory(string? path, Exception e)
        => AppendInMemory($"Error writing log file '{path}': {e.Message}");

    public void Write(string message, LogMessageSeverity severity = LogMessageSeverity.Info)
    {
        AppendInMemory(message);
        FileLog?.Append(message);
        logService?.Report(message, severity);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(TraceLog traceLog)
    {
        internal string[] Entries => traceLog._log;
    }
}

