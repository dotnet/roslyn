// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command-line analyzer assembly dependency specification.
    /// </summary>
    public struct CommandLineAnalyzerDependency : IEquatable<CommandLineAnalyzerDependency>
    {
        public CommandLineAnalyzerDependency(string path)
        {
            FilePath = path;
        }

        /// <summary>
        /// Assembly file path.
        /// </summary>
        public string FilePath { get; }

        public override bool Equals(object obj)
        {
            return obj is CommandLineAnalyzerDependency && base.Equals((CommandLineAnalyzerDependency)obj);
        }

        public bool Equals(CommandLineAnalyzerDependency other)
        {
            return FilePath == other.FilePath;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(FilePath, 0);
        }
    }
}
