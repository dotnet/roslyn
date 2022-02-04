// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CommandLineDiagnosticFormatter : CSharpDiagnosticFormatter
    {
        private readonly string _baseDirectory;
        private readonly Lazy<string> _lazyNormalizedBaseDirectory;
        private readonly bool _displayFullPaths;
        private readonly bool _displayEndLocations;

        internal CommandLineDiagnosticFormatter(string baseDirectory, bool displayFullPaths, bool displayEndLocations)
        {
            _baseDirectory = baseDirectory;
            _displayFullPaths = displayFullPaths;
            _displayEndLocations = displayEndLocations;
            _lazyNormalizedBaseDirectory = new Lazy<string>(() => FileUtilities.TryNormalizeAbsolutePath(baseDirectory));
        }

        internal override string FormatSourceSpan(LinePositionSpan span, IFormatProvider formatter)
        {
            if (_displayEndLocations)
            {
                return string.Format(formatter, "({0},{1},{2},{3})",
                    span.Start.Line + 1,
                    span.Start.Character + 1,
                    span.End.Line + 1,
                    span.End.Character + 1);
            }
            else
            {
                return string.Format(formatter, "({0},{1})",
                    span.Start.Line + 1,
                    span.Start.Character + 1);
            }
        }

        internal override string FormatSourcePath(string path, string basePath, IFormatProvider formatter)
        {
            var normalizedPath = FileUtilities.NormalizeRelativePath(path, basePath, _baseDirectory);
            if (normalizedPath == null)
            {
                return path;
            }

            // By default, specify the name of the file in which an error was found.
            // When The /fullpaths option is present, specify the full path to the file.
            return _displayFullPaths ? normalizedPath : RelativizeNormalizedPath(normalizedPath);
        }

        /// <summary>
        /// Get the path name starting from the <see cref="_baseDirectory"/>
        /// </summary>
        internal string RelativizeNormalizedPath(string normalizedPath)
        {
            var normalizedBaseDirectory = _lazyNormalizedBaseDirectory.Value;
            if (normalizedBaseDirectory == null)
            {
                return normalizedPath;
            }

            var normalizedDirectory = PathUtilities.GetDirectoryName(normalizedPath);
            if (PathUtilities.IsSameDirectoryOrChildOf(normalizedDirectory, normalizedBaseDirectory))
            {
                return normalizedPath.Substring(
                    PathUtilities.IsDirectorySeparator(normalizedBaseDirectory.Last())
                        ? normalizedBaseDirectory.Length
                        : normalizedBaseDirectory.Length + 1);
            }

            return normalizedPath;
        }
    }
}
