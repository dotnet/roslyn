// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rebuild
{
    /// <summary>
    /// For most operations the rebuild scenario does not need to provide a 
    /// <see cref="SourceReferenceResolver"/>. However the usage of #line in the program 
    /// forces our hand. 
    /// 
    /// A #line pragma which has a document argument goes through the same path normalization
    /// as other files. This is always done relative to the file which contains the #line 
    /// pragma (the containing file will be the base path).
    /// 
    /// It is possible for multiple files, in different directories, to have a #line which
    /// refers to the same file. For example lib1.cs and dir/lib2.cs can both have the following
    /// entry:
    ///
    ///     #line 42 "data.txt"
    /// 
    /// Without a resolver entry to provide a normalized path, based on the directory, it is possible
    /// these get normalized out to the same path. Particularly when pathmap is involved and we
    /// are observing unix paths in a windows rebuild (or vice versa). This is because pathmap can 
    /// create paths which are illegal to the current operating system (by design).
    /// </summary>
    internal sealed class RebuildSourceReferenceResolver : SourceReferenceResolver
    {
        internal static RebuildSourceReferenceResolver Instance { get; } = new RebuildSourceReferenceResolver();

        private RebuildSourceReferenceResolver()
        {
        }

        public override bool Equals(object? other) => object.ReferenceEquals(this, other);

        public override int GetHashCode() => 0;

        public override string? NormalizePath(string path, string? baseFilePath)
        {
            if (baseFilePath is null)
            {
                return path;
            }

            // The only invariant we need to maintain here is that for a given external file identified
            // via #line directive across many source files we always return the same name for that 
            // file. What name we return is irrelevant, it just needs to be the same. The actual name 
            // return here is eventually discarded and we end up writing the name from the PDB. 
            var index = baseFilePath.LastIndexOfAny(new[] { '/', '\\' });
            if (index > 0)
            {
                var root = baseFilePath.Substring(0, index);
                return @$"{root}\{path}";
            }

            return null;
        }

        public override Stream OpenRead(string resolvedPath) => throw ExceptionUtilities.Unreachable();

        public override string? ResolveReference(string path, string? baseFilePath) => throw ExceptionUtilities.Unreachable();
    }

}
