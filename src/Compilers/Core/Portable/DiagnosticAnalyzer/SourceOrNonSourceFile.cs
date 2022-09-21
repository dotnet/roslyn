// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a source file or an additional file.
    /// For source files, <see cref="SourceTree"/> is non-null and <see cref="AdditionalFile"/> is null.
    /// For additional files, <see cref="AdditionalFile"/> is non-null and <see cref="SourceTree"/> is null.
    /// </summary>
    internal readonly struct SourceOrNonSourceFile
        : IEquatable<SourceOrNonSourceFile>
    {
        public SyntaxTree? SourceTree { get; }
        public AdditionalText? AdditionalFile { get; }
        public AdditionalText? AnalyzerConfigFile { get; }

        public SourceOrNonSourceFile(SyntaxTree tree)
        {
            SourceTree = tree;
            AdditionalFile = null;
            AnalyzerConfigFile = null;
        }

        public SourceOrNonSourceFile(AdditionalText file, bool isAnalyzerConfigFile)
        {
            AdditionalFile = isAnalyzerConfigFile ? null : file;
            AnalyzerConfigFile = isAnalyzerConfigFile ? file : null;
            SourceTree = null;
        }

        public override bool Equals(object? obj)
            => obj is SourceOrNonSourceFile file && Equals(file);

        public bool Equals(SourceOrNonSourceFile other)
            => SourceTree == other.SourceTree && AdditionalFile == other.AdditionalFile && AnalyzerConfigFile == other.AnalyzerConfigFile;

        public static bool operator ==(SourceOrNonSourceFile left, SourceOrNonSourceFile right)
            => Equals(left, right);

        public static bool operator !=(SourceOrNonSourceFile left, SourceOrNonSourceFile right)
            => !Equals(left, right);

        public override int GetHashCode()
        {
            if (SourceTree != null)
            {
                return Hash.Combine(0, SourceTree.GetHashCode());
            }
            else if (AdditionalFile != null)
            {
                return Hash.Combine(1, AdditionalFile.GetHashCode());
            }
            else
            {
                RoslynDebug.Assert(AnalyzerConfigFile != null);
                return Hash.Combine(2, AnalyzerConfigFile.GetHashCode());
            }
        }
    }
}
