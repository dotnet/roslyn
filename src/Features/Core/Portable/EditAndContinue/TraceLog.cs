// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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
            public readonly object Object;
            public readonly int Int32;

            public Arg(object value)
            {
                Int32 = 0;
                Object = value ?? "<null>";
            }

            public Arg(int value)
            {
                Int32 = value;
                Object = null;
            }

            public override string ToString() => (Object is null) ? Int32.ToString() : Object.ToString();

            public static implicit operator Arg(string value) => new Arg(value);
            public static implicit operator Arg(int value) => new Arg(value);
            public static implicit operator Arg(ProjectId value) => new Arg(value.Id.GetHashCode());
            public static implicit operator Arg(ProjectAnalysisSummary value) => new Arg(ToString(value));
            public static implicit operator Arg(Diagnostic value) => new Arg(value);

            private static string ToString(ProjectAnalysisSummary summary)
                => summary switch
                {
                    ProjectAnalysisSummary.CompilationErrors => nameof(ProjectAnalysisSummary.CompilationErrors),
                    ProjectAnalysisSummary.NoChanges => nameof(ProjectAnalysisSummary.NoChanges),
                    ProjectAnalysisSummary.RudeEdits => nameof(ProjectAnalysisSummary.RudeEdits),
                    ProjectAnalysisSummary.ValidChanges => nameof(ProjectAnalysisSummary.ValidChanges),
                    ProjectAnalysisSummary.ValidInsignificantChanges => nameof(ProjectAnalysisSummary.ValidInsignificantChanges),
                    _ => null,
                };
        }

        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        internal readonly struct Entry
        {
            public readonly string MessageFormat;
            public readonly Arg[] ArgsOpt;

            public Entry(string format, Arg[] argsOpt)
            {
                MessageFormat = format;
                ArgsOpt = argsOpt;
            }

            internal string GetDebuggerDisplay() =>
                (MessageFormat == null) ? "" : string.Format(MessageFormat, ArgsOpt?.Select(a => (object)a).ToArray() ?? Array.Empty<object>());
        }

        private readonly Entry[] _log;
        private readonly string _id;
        private int _currentLine;

        public TraceLog(int logSize, string id)
        {
            _log = new Entry[logSize];
            _id = id;
        }

        private void Append(Entry entry)
        {
            var index = Interlocked.Increment(ref _currentLine);
            _log[(index - 1) % _log.Length] = entry;
        }

        public void Write(string str) => Write(str, null);

        public void Write(string format, params Arg[] args)
            => Append(new Entry(format, args));

        [Conditional("DEBUG")]
        public void DebugWrite(string str) => DebugWrite(str, null);

        [Conditional("DEBUG")]
        public void DebugWrite(string format, params Arg[] args)
        {
            var entry = new Entry(format, args);
            Append(entry);
            Debug.WriteLine(entry.ToString(), _id);
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly TraceLog _traceLog;

            public TestAccessor(TraceLog traceLog)
                => _traceLog = traceLog;

            internal Entry[] Entries => _traceLog._log;
        }
    }
}
