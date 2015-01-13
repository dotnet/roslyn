// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CommandLineDiagnosticFormatter : CSharpDiagnosticFormatter
    {
        private readonly string baseDirectory;
        private Lazy<string> lazyNormalizedBaseDirectory;
        private readonly bool displayFullPaths;
        private readonly bool displayEndLocations;

        internal CommandLineDiagnosticFormatter(string baseDirectory, bool displayFullPaths, bool displayEndLocations)
        {
            this.baseDirectory = baseDirectory;
            this.displayFullPaths = displayFullPaths;
            this.displayEndLocations = displayEndLocations;
            this.lazyNormalizedBaseDirectory = new Lazy<string>(() => FileUtilities.TryNormalizeAbsolutePath(baseDirectory));
        }

        internal override string FormatSourceSpan(LinePositionSpan span, IFormatProvider formatter)
        {
            if (displayEndLocations)
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
            var normalizedPath = FileUtilities.NormalizeRelativePath(path, basePath, baseDirectory);
            if (normalizedPath == null)
            {
                return path;
            }

            // By default, specify the name of the file in which an error was found.
            // When The /fullpaths option is present, specify the full path to the file.
            return displayFullPaths ? normalizedPath : RelativizeNormalizedPath(normalizedPath);
        }

        /// <summary>
        /// Get the path name starting from the <see cref="baseDirectory"/>
        /// </summary>
        internal string RelativizeNormalizedPath(string normalizedPath)
        {
            var normalizedBaseDirectory = lazyNormalizedBaseDirectory.Value;
            if (normalizedBaseDirectory == null)
            {
                return normalizedPath;
            }

            var directory = PathUtilities.GetDirectoryName(normalizedPath);
            if (string.Compare(directory, 0, normalizedBaseDirectory, 0, normalizedBaseDirectory.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return normalizedPath.Substring(normalizedBaseDirectory.Length + 1);
            }

            return normalizedPath;
        }
    }
}
