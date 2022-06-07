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
        public void WriteReadPaths(TextWriter s)
        {
            var temp = new string[_readFiles.Count];
            int i = 0;
            var readFiles = Interlocked.Exchange(
                ref _readFiles,
                null!);
            foreach (var path in readFiles)
            {
                temp[i] = path.ToUpperInvariant();
                i++;
            }
            Array.Sort<string>(temp);

            foreach (var path in temp)
            {
                s.WriteLine(path);
            }
        }

        /// <summary>
        /// Writes all of the paths the TouchedFileLogger to the given 
        /// TextWriter in upper case. After calling this method the
        /// logger is in an undefined state.
        /// </summary>
        public void WriteWrittenPaths(TextWriter s)
        {
            var temp = new string[_writtenFiles.Count];
            int i = 0;
            var writtenFiles = Interlocked.Exchange(
                ref _writtenFiles,
                null!);
            foreach (var path in writtenFiles)
            {
                temp[i] = path.ToUpperInvariant();
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
