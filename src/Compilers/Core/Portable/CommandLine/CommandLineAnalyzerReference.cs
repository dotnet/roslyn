// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line analyzer assembly specification.
    /// </summary>
    public struct CommandLineAnalyzerReference : IEquatable<CommandLineAnalyzerReference>
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

        public override bool Equals(object obj)
        {
            return obj is CommandLineAnalyzerReference && base.Equals((CommandLineAnalyzerReference)obj);
        }

        public bool Equals(CommandLineAnalyzerReference other)
        {
            return _path == other._path;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_path, 0);
        }
    }
}
