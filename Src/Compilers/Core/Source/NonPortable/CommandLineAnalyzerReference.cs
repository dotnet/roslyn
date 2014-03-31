// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line analyzer assembly specification.
    /// </summary>
    public struct CommandLineAnalyzerReference : IEquatable<CommandLineAnalyzerReference>
    {
        private readonly string path;

        public CommandLineAnalyzerReference(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Assembly file path.
        /// </summary>
        public string FilePath
        {
            get
            {
                return path;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is CommandLineAnalyzerReference && base.Equals((CommandLineAnalyzerReference)obj);
        }

        public bool Equals(CommandLineAnalyzerReference other)
        {
            return this.path == other.path;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(path, 0);
        }
    }
}
