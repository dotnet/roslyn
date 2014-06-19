// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a source file specification stored on command line arguments.
    /// </summary>
    public struct CommandLineSourceFile
    {
        private readonly string path;
        private readonly bool isScript;

        internal CommandLineSourceFile(string path, bool isScript)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            this.path = path;
            this.isScript = isScript;
        }

        /// <summary>
        /// Resolved absolute path of the source file (does not contain wildcards).
        /// </summary>
        /// <remarks>
        /// Although this path is absolute it may not be normalized. That is, it may contain ".." and "." in the middle. 
        /// </remarks>
        public string Path
        {
            get { return path; }
        }

        /// <summary>
        /// True if the file should be treated as a script file.
        /// </summary>
        public bool IsScript
        {
            get { return isScript; }
        }
    }
}
