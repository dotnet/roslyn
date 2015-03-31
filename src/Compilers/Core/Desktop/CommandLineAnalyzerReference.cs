// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line analyzer assembly specification.
    /// </summary>
    public struct CommandLineAnalyzerReference
    {
        private readonly string _path;

        public CommandLineAnalyzerReference(string path)
        {
            _path = path;
        }

        /// <summary>
        /// Assembly file path.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _path;
            }
        }
    }
}
