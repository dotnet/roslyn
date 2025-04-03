// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provider that allows analyzers to easily find and use
    /// <see href="https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Using%20Additional%20Files.md">additional files</see>.
    /// </summary>
    internal sealed class AdditionalFileProvider
    {
        private readonly ImmutableArray<AdditionalText> _additionalFiles;

        internal AdditionalFileProvider(ImmutableArray<AdditionalText> additionalFiles)
        {
            _additionalFiles = additionalFiles;
        }

        /// <summary>
        /// Creates an instance of this provider from the specified <see cref="AnalyzerOptions"/>.
        /// </summary>
        /// <param name="options">Options passed to a <see cref="DiagnosticAnalyzer"/>.</param>
        /// <returns>An instance of <see cref="AdditionalFileProvider"/>.</returns>
        public static AdditionalFileProvider FromOptions(AnalyzerOptions options)
            => new(options.AdditionalFiles);

        /// <summary>
        /// Returns the first additional file whose name is the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">Name of the file, including extension, to return.</param>
        /// <returns>An additional file or <c>null</c> if no file can be found.</returns>
        public AdditionalText? GetFile(string fileName)
            => _additionalFiles.FirstOrDefault(x => Path.GetFileName(x.Path).Equals(fileName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns all additional files whose names match the specified <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">A regular expression.</param>
        /// <returns>An enumeration of additional files whose names match the pattern.</returns>
        public IEnumerable<AdditionalText> GetMatchingFiles(string pattern)
            => _additionalFiles.Where(x => Regex.IsMatch(Path.GetFileName(x.Path), pattern, RegexOptions.IgnoreCase));
    }
}
