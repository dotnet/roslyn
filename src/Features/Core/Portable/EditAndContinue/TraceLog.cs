// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Fixed size rolling tracing log. 
    /// </summary>
    /// <remarks>
    /// Recent entries are captured in a memory dump.
    /// If DEBUG is defined, all entries written to <see cref="DebugWrite(string)"/> or
    /// <see cref="DebugWrite(string, Arg[])"/> are print to <see cref="Debug"/> output.
    /// </remarks>
    internal sealed class TraceLog
    {
        internal readonly struct Arg
        {
            // To display enums in Expression Evaluator we need to remember the type of the enum.
            // The debugger currently does not support evaluating expressions that involve Type instances nor lambdas,
            // so we need to manually special case the types of enums we care about displaying.

            private enum EnumType
            {
                ProjectAnalysisSummary,
                RudeEditKind,
                ModuleUpdateStatus,
                EditAndContinueCapabilities,
            }

            private static readonly StrongBox<EnumType> s_ProjectAnalysisSummary = new(EnumType.ProjectAnalysisSummary);
            private static readonly StrongBox<EnumType> s_RudeEditKind = new(EnumType.RudeEditKind);
            private static readonly StrongBox<EnumType> s_ModuleUpdateStatus = new(EnumType.ModuleUpdateStatus);
            private static readonly StrongBox<EnumType> s_EditAndContinueCapabilities = new(EnumType.EditAndContinueCapabilities);

            public readonly object? Object;
            public readonly int Int32;
            public readonly ImmutableArray<int> Tokens;

            public Arg(object? value)
            {
                Int32 = -1;
                Object = value ?? "<null>";
                Tokens = default;
            }

            public Arg(ImmutableArray<int> tokens)
            {
                Int32 = -1;
                Object = null;
                Tokens = tokens;
            }

            private Arg(int value, StrongBox<EnumType> enumKind)
            {
                Int32 = value;
                Object = enumKind;
                Tokens = default;
            }

            public object? GetDebuggerDisplay()
                => (!Tokens.IsDefault) ? string.Join(",", Tokens.Select(token => token.ToString("X8"))) :
                   (Object is null) ? Int32 :
                   (Object is StrongBox<EnumType> { Value: var enumType }) ? enumType switch
                   {
                       EnumType.ProjectAnalysisSummary => (ProjectAnalysisSummary)Int32,
                       EnumType.RudeEditKind => (RudeEditKind)Int32,
                       EnumType.ModuleUpdateStatus => (ModuleUpdateStatus)Int32,
                       EnumType.EditAndContinueCapabilities => (EditAndContinueCapabilities)Int32,
                       _ => throw ExceptionUtilities.UnexpectedValue(enumType)
                   } :
                   Object;

            public static implicit operator Arg(string? value) => new(value);
            public static implicit operator Arg(int value) => new(value);
            public static implicit operator Arg(bool value) => new(value ? "true" : "false");
            public static implicit operator Arg(ProjectId value) => new(value.DebugName);
            public static implicit operator Arg(DocumentId value) => new(value.DebugName);
            public static implicit operator Arg(Diagnostic value) => new(value);
            public static implicit operator Arg(ProjectAnalysisSummary value) => new((int)value, s_ProjectAnalysisSummary);
            public static implicit operator Arg(RudeEditKind value) => new((int)value, s_RudeEditKind);
            public static implicit operator Arg(ModuleUpdateStatus value) => new((int)value, s_ModuleUpdateStatus);
            public static implicit operator Arg(EditAndContinueCapabilities value) => new((int)value, s_EditAndContinueCapabilities);
            public static implicit operator Arg(ImmutableArray<int> tokens) => new(tokens);
        }

        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        internal readonly struct Entry
        {
            public readonly string MessageFormat;
            public readonly Arg[]? Args;

            public Entry(string format, Arg[]? args)
            {
                MessageFormat = format;
                Args = args;
            }

            internal string GetDebuggerDisplay() =>
                (MessageFormat == null) ? "" : string.Format(MessageFormat, Args?.Select(a => a.GetDebuggerDisplay()).ToArray() ?? Array.Empty<object>());
        }

        private readonly Entry[] _log;
        private readonly string _id;
        private readonly string? _logDirectory;
        private int _currentLine;

        public TraceLog(int logSize, string id, string? logDirectory)
        {
            _log = new Entry[logSize];
            _id = id;
            _logDirectory = logDirectory;
        }

        public bool FileLoggingEnabled
            => _logDirectory != null;

        private void AppendInMemory(Entry entry)
        {
            var index = Interlocked.Increment(ref _currentLine);
            _log[(index - 1) % _log.Length] = entry;
        }

        private void AppendFileLoggingErrorInMemory(string? path, Exception e)
            => AppendInMemory(new Entry("Error writing log file '{0}': {1}", new[] { new Arg(path), new Arg(e.Message) }));

        private void AppendToFile(Entry entry)
        {
            Debug.Assert(_logDirectory != null);
            string? path = null;

            try
            {
                path = Path.Combine(_logDirectory, "Trace.log");
                File.AppendAllLines(path, new[] { entry.GetDebuggerDisplay() });
            }
            catch (Exception e)
            {
                AppendFileLoggingErrorInMemory(path, e);
            }
        }

        private void Append(Entry entry)
        {
            AppendInMemory(entry);

            if (_logDirectory != null)
            {
                AppendToFile(entry);
            }
        }

        public void Write(string str)
            => Write(str, args: null);

        public void Write(string format, params Arg[]? args)
            => Append(new Entry(format, args));

        [Conditional("DEBUG")]
        public void DebugWrite(string str)
            => DebugWrite(str, args: null);

        [Conditional("DEBUG")]
        public void DebugWrite(string format, params Arg[]? args)
        {
            var entry = new Entry(format, args);
            Append(entry);
            Debug.WriteLine(entry.ToString(), _id);
        }

        private string CreateSessionDirectory(DebuggingSessionId sessionId, string relativePath)
        {
            Contract.ThrowIfNull(_logDirectory);
            var directory = Path.Combine(_logDirectory, sessionId.Ordinal.ToString(), relativePath);
            Directory.CreateDirectory(directory);
            return directory;
        }

        private string MakeSourceFileLogPath(Document document, string suffix, DebuggingSessionId sessionId, int generation)
        {
            Debug.Assert(document.FilePath != null);
            Debug.Assert(document.Project.FilePath != null);

            var projectDir = PathUtilities.GetDirectoryName(document.Project.FilePath)!;
            var documentDir = PathUtilities.GetDirectoryName(document.FilePath)!;
            var extension = PathUtilities.GetExtension(document.FilePath);
            var fileName = PathUtilities.GetFileName(document.FilePath, includeExtension: false);

            var relativeDir = PathUtilities.IsSameDirectoryOrChildOf(documentDir, projectDir) ? PathUtilities.GetRelativePath(projectDir, documentDir) : documentDir;
            relativeDir = relativeDir.Replace('\\', '_').Replace('/', '_');

            var directory = CreateSessionDirectory(sessionId, Path.Combine(document.Project.Name, relativeDir));
            return Path.Combine(directory, $"{fileName}.{generation}.{suffix}{extension}");
        }

        public void WriteToFile(DebuggingSessionId sessionId, ImmutableArray<byte> bytes, string directory, string fileName)
        {
            string? path = null;
            try
            {
                path = Path.Combine(CreateSessionDirectory(sessionId, directory), fileName);
                File.WriteAllBytes(path, bytes.ToArray());
            }
            catch (Exception e)
            {
                AppendFileLoggingErrorInMemory(path, e);
            }
        }

        public async ValueTask WriteToFileAsync(Func<Stream, CancellationToken, ValueTask> writer, DebuggingSessionId sessionId, string directory, string fileName, CancellationToken cancellationToken)
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
                AppendFileLoggingErrorInMemory(path, e);
            }
        }

        private async ValueTask WriteDocumentAsync(Document document, string fileNameSuffix, DebuggingSessionId sessionId, int generation, CancellationToken cancellationToken)
        {
            Debug.Assert(document.FilePath != null);

            string? path = null;
            try
            {
                path = MakeSourceFileLogPath(document, fileNameSuffix, sessionId, generation);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write | FileShare.Delete);
                using var writer = new StreamWriter(file, text.Encoding ?? Encoding.UTF8);
                text.Write(writer, cancellationToken);
            }
            catch (Exception e)
            {
                AppendFileLoggingErrorInMemory(path, e);
            }
        }

        public async ValueTask WriteDocumentChangeAsync(Document? oldDocument, Document? newDocument, DebuggingSessionId sessionId, int generation, CancellationToken cancellationToken)
        {
            if (oldDocument?.FilePath != null)
            {
                await WriteDocumentAsync(oldDocument, fileNameSuffix: "old", sessionId, generation, cancellationToken).ConfigureAwait(false);
            }

            if (newDocument?.FilePath != null)
            {
                await WriteDocumentAsync(newDocument, fileNameSuffix: "new", sessionId, generation, cancellationToken).ConfigureAwait(false);
            }
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly TraceLog _traceLog;

            public TestAccessor(TraceLog traceLog)
                => _traceLog = traceLog;

            internal Entry[] Entries => _traceLog._log;
        }
    }
}
