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
    internal readonly struct SourceOrAdditionalFile
        : IEquatable<SourceOrAdditionalFile>
    {
        public SyntaxTree? SourceTree { get; }
        public AdditionalText? AdditionalFile { get; }

        public SourceOrAdditionalFile(SyntaxTree tree)
        {
            SourceTree = tree;
            AdditionalFile = null;
        }

        public SourceOrAdditionalFile(AdditionalText file)
        {
            AdditionalFile = file;
            SourceTree = null;
        }

        public override bool Equals(object? obj)
            => obj is SourceOrAdditionalFile file && Equals(file);

        public bool Equals(SourceOrAdditionalFile other)
            => SourceTree == other.SourceTree && AdditionalFile == other.AdditionalFile;

        public static bool operator ==(SourceOrAdditionalFile left, SourceOrAdditionalFile right)
            => Equals(left, right);

        public static bool operator !=(SourceOrAdditionalFile left, SourceOrAdditionalFile right)
            => !Equals(left, right);

        public override int GetHashCode()
        {
            if (SourceTree != null)
            {
                return Hash.Combine(true, SourceTree.GetHashCode());
            }
            else
            {
                RoslynDebug.Assert(AdditionalFile != null);
                return Hash.Combine(false, AdditionalFile.GetHashCode());
            }
        }
    }
}
