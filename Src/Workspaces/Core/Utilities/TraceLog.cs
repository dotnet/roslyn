// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Fixed size rolling tracing log. 
    /// </summary>
    /// <remarks>
    /// Recent entries are captured in a memory dump. 
    /// All entries are printed out to <see cref="Trace"/> output or <see cref="Debug"/> output.
    /// </remarks>
    internal sealed class TraceLog
    {
        private readonly string[] log;
        private readonly string id;
        private int currentLine;

        public TraceLog(int logSize, string id)
        {
            this.log = new string[logSize];
            this.id = id;
        }

        private void Append(string str)
        {
            int index = Interlocked.Increment(ref currentLine);
            log[(index - 1) % log.Length] = str;
        }

        public void Write(string str)
        {
            Append(str);
            Trace.WriteLine(str, id);
        }

        [Conditional("DEBUG")]
        public void DebugWrite(string str)
        {
            Append(str);
            Debug.WriteLine(str, id);
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
