// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line analyzer assembly specification.
    /// </summary>
    [DebuggerDisplay("{FilePath,nq}")]
    public readonly struct CommandLineAnalyzerReference : IEquatable<CommandLineAnalyzerReference>
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

        public override bool Equals(object? obj)
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
