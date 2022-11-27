// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used for logging all the paths which are "touched" (used) in any way
    /// in the process of compilation.
    /// </summary>
    internal class TouchedFileLogger
    {
        private ConcurrentSet<string> _readFiles;
        private ConcurrentSet<string> _writtenFiles;

        public TouchedFileLogger()
        {
            _readFiles = new ConcurrentSet<string>();
            _writtenFiles = new ConcurrentSet<string>();
        }

        /// <summary>
        /// Adds a fully-qualified path to the Logger for a read file.
        /// Semantics are undefined after a call to <see cref="WriteReadPaths(TextWriter)" />.
        /// </summary>
        public void AddRead(string path)
        {
            if (path == null) throw new ArgumentNullException(path);
            _readFiles.Add(path);
        }

        /// <summary>
        /// Adds a fully-qualified path to the Logger for a written file.
        /// Semantics are undefined after a call to <see cref="WriteWrittenPaths(TextWriter)" />.
        /// </summary>
        public void AddWritten(string path)
        {
            if (path == null) throw new ArgumentNullException(path);
            _writtenFiles.Add(path);
        }

        /// <summary>
        /// Adds a fully-qualified path to the Logger for a read and written
        /// file. Semantics are undefined after a call to
        /// <see cref="WriteWrittenPaths(TextWriter)" />.
        /// </summary>
        public void AddReadWritten(string path)
        {
            AddRead(path);
            AddWritten(path);
        }

        /// <summary>
        /// Writes all of the paths the TouchedFileLogger to the given 
        /// TextWriter in upper case. After calling this method the
        /// logger is in an undefined state.
        /// </summary>
        public void WriteReadPaths(TextWriter s) => WritePathSet(s, ref _readFiles);

        /// <summary>
        /// Writes all of the paths the TouchedFileLogger to the given 
        /// TextWriter in upper case. After calling this method the
        /// logger is in an undefined state.
        /// </summary>
        public void WriteWrittenPaths(TextWriter s) => WritePathSet(s, ref _writtenFiles);

        private void WritePathSet(TextWriter s, ref ConcurrentSet<string> pathSet)
        {
            var temp = new string[pathSet.Count];
            int i = 0;
            var paths = Interlocked.Exchange(
                ref pathSet,
                null!);
            foreach (var path in paths)
            {
                temp[i] = path;
                i++;
            }
            Array.Sort<string>(temp);

            foreach (var path in temp)
            {
                s.WriteLine(path);
            }
        }
    }
}
