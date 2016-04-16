// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Fixed size rolling tracing log. 
    /// </summary>
    /// <remarks>
    /// Recent entries are captured in a memory dump.
    /// If DEBUG is defined, all entries written to <see cref="DebugWrite(string)"/> or
    /// <see cref="DebugWrite(string, object[])"/> are print to <see cref="Debug"/> output.
    /// </remarks>
    internal sealed class TraceLog
    {
        private readonly string[] _log;
        private readonly string _id;
        private int _currentLine;

        public TraceLog(int logSize, string id)
        {
            _log = new string[logSize];
            _id = id;
        }

        private void Append(string str)
        {
            int index = Interlocked.Increment(ref _currentLine);
            _log[(index - 1) % _log.Length] = str;
        }

        public void Write(string str)
        {
            Append(str);
        }

        [Conditional("DEBUG")]
        public void DebugWrite(string str)
        {
            Append(str);
            Debug.WriteLine(str, _id);
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        [Conditional("DEBUG")]
        public void DebugWrite(string format, params object[] args)
        {
            DebugWrite(string.Format(format, args));
        }
    }
}
