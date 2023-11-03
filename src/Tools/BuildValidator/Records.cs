// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Text;

namespace BuildValidator
{
    internal sealed record AssemblyInfo(string FilePath, Guid Mvid)
    {
        internal string FileName => Path.GetFileName(FilePath);
        internal string TargetFramework => Path.GetFileName(Path.GetDirectoryName(FilePath))!;
    }

    internal sealed record PortableExecutableInfo(string FilePath, Guid Mvid, bool IsReadyToRun, bool IsReferenceAssembly);

    internal record Options(
        string[] AssembliesPaths,
        string[] ReferencesPaths,
        string[] Excludes,
        string SourcePath,
        bool Verbose,
        bool Quiet,
        bool Debug,
        string DebugPath);

    /// <summary>An entry in the source-link.json dictionary.</summary>
    public record SourceLinkEntry
    {
        public string Prefix { get; }
        public string Replace { get; }

        public SourceLinkEntry(string prefix, string replace)
        {
            Prefix = prefix;
            Replace = replace;
        }
    }
}
