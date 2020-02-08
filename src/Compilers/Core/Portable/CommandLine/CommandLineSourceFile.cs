﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a source file specification stored on command line arguments.
    /// </summary>
    [DebuggerDisplay("{Path,nq}")]
    public struct CommandLineSourceFile
    {
        private readonly string _path;
        private readonly bool _isScript;

        public CommandLineSourceFile(string path, bool isScript)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            _path = path;
            _isScript = isScript;
        }

        /// <summary>
        /// Resolved absolute path of the source file (does not contain wildcards).
        /// </summary>
        /// <remarks>
        /// Although this path is absolute it may not be normalized. That is, it may contain ".." and "." in the middle. 
        /// </remarks>
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// True if the file should be treated as a script file.
        /// </summary>
        public bool IsScript
        {
            get { return _isScript; }
        }
    }
}
