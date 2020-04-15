// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Paths of files produced by the compilation.
    /// </summary>
    public readonly struct CompilationOutputFilePaths : IEquatable<CompilationOutputFilePaths>, IObjectWritable
    {
        /// <summary>
        /// Full path to the assembly or module produced by the compilation, or <see langword="null"/> if unknown.
        /// </summary>
        public readonly string? AssemblyPath { get; }

        bool IObjectWritable.ShouldReuseInSerialization => throw new NotImplementedException();

        // TODO: https://github.com/dotnet/roslyn/issues/35065
        // The project system doesn't currently provide paths to the PDB or XML files that the compiler produces.
        // public readonly string? PdbPath { get; }
        // public readonly string? DocumentationCommentsPath { get; }

        internal CompilationOutputFilePaths(string? assemblyPath)
        {
            AssemblyPath = assemblyPath;
        }

        public CompilationOutputFilePaths WithAssemblyPath(string? path)
            => new CompilationOutputFilePaths(assemblyPath: path);

        public override bool Equals(object? obj)
            => obj is CompilationOutputFilePaths paths && Equals(paths);

        public bool Equals(CompilationOutputFilePaths other)
            => AssemblyPath == other.AssemblyPath;

        public override int GetHashCode()
            => AssemblyPath?.GetHashCode() ?? 0;

        public static bool operator ==(in CompilationOutputFilePaths left, in CompilationOutputFilePaths right)
            => left.Equals(right);

        public static bool operator !=(in CompilationOutputFilePaths left, in CompilationOutputFilePaths right)
            => !left.Equals(right);

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteString(AssemblyPath);
        }

        internal static CompilationOutputFilePaths ReadFrom(ObjectReader reader)
        {
            var assemblyPath = reader.ReadString();
            return new CompilationOutputFilePaths(assemblyPath);
        }
    }
}
