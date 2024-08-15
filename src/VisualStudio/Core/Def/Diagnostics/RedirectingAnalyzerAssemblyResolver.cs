// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;

[Export(typeof(IAnalyzerAssemblyResolver)), Shared]
internal sealed class RedirectingAnalyzerAssemblyResolver : IAnalyzerAssemblyResolver
{
    private readonly VisualStudioWorkspaceImpl _workspace;
    private readonly Lazy<ImmutableArray<Matcher>> _matchers;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RedirectingAnalyzerAssemblyResolver(VisualStudioWorkspaceImpl workspace)
    {
        _workspace = workspace;
        _matchers = new(CreateMatchers);
    }

    public Assembly? ResolveAssembly(AssemblyName assemblyName, string assemblyOriginalDirectory)
    {
        foreach (var matcher in _matchers.Value)
        {
            if (matcher.TryRedirect(assemblyOriginalDirectory) is { } redirectedDirectory)
            {
                var redirectedPath = Path.Combine(matcher.TargetDirectory, redirectedDirectory, assemblyName.Name + ".dll");
                return Assembly.LoadFile(redirectedPath);
            }
        }

        return null;
    }

    private ImmutableArray<Matcher> CreateMatchers()
    {
        var mappingFilePaths = _workspace.LazyInsertedAnalyzerProvider?.GetInsertedAnalyzerMappingFilePaths() ?? [];

        if (mappingFilePaths.IsDefaultOrEmpty)
        {
            return [];
        }

        var builder = ArrayBuilder<Matcher>.GetInstance();

        foreach (var mappingFilePath in mappingFilePaths)
        {
            if (!File.Exists(mappingFilePath))
            {
                continue;
            }

            var analyzerDir = Path.GetDirectoryName(mappingFilePath);

            var mappings = File.ReadAllLines(mappingFilePath);

            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping) ||
                    mapping.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var normalized = PathUtilities.NormalizeWithForwardSlash(mapping);

                if (normalized.IndexOf("/*/", StringComparison.Ordinal) is var starIndex and >= 0)
                {
                    var prefix = normalized[..starIndex];
                    var suffix = normalized[(starIndex + 3)..];
                    builder.Add(new Matcher
                    {
                        TargetDirectory = analyzerDir,
                        Prefix = prefix,
                        Suffix = suffix,
                    });
                    continue;
                }

                builder.Add(new Matcher
                {
                    TargetDirectory = analyzerDir,
                    Prefix = normalized,
                });
            }
        }

        return builder.ToImmutableAndFree();
    }

    private readonly record struct Matcher
    {
        public required string TargetDirectory { get; init; }
        public required string Prefix { get; init; }
        public string? Suffix { get; init; }

        public string? TryRedirect(string directory)
        {
            directory = PathUtilities.NormalizeWithForwardSlash(directory);

            for (var startIndex = 0; startIndex < directory.Length;)
            {
                var prefixStart = directory.IndexOf(Prefix, startIndex, StringComparison.OrdinalIgnoreCase);

                if (prefixStart <= 0)
                {
                    break;
                }

                if (directory[prefixStart - 1] == '/')
                {
                    if (Suffix is null)
                    {
                        if (directory.Length == prefixStart + Prefix.Length || directory[prefixStart + Prefix.Length + 1] == '/')
                        {
                            return Prefix;
                        }
                    }
                    else
                    {
                        if (directory.AsSpan(prefixStart + Prefix.Length) is ['/', ..] &&
                            directory.IndexOf('/', prefixStart + Prefix.Length + 1) is var suffixStart and >= 0 &&
                            directory.AsSpan(suffixStart + 1).StartsWith(Suffix.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            var versionStart = prefixStart + Prefix.Length + 1;
                            var version = directory.AsSpan(versionStart, suffixStart - versionStart);

                            if (version.IndexOf('.') is var dotIndex and >= 0)
                            {
                                return Prefix + '/' + version[..dotIndex].ToString() + '/' + Suffix;
                            }
                        }
                    }
                }

                startIndex = prefixStart + 1;
            }

            return null;
        }
    }
}
