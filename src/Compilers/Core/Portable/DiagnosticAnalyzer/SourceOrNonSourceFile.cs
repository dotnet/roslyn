// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a source file or a non-source file.
    /// For source files, <see cref="SourceTree"/> is non-null and <see cref="NonSourceFile"/> is null.
    /// For non-source files, <see cref="NonSourceFile"/> is non-null and <see cref="SourceTree"/> is null.
    /// </summary>
    internal sealed class SourceOrNonSourceFile
        : IEquatable<SourceOrNonSourceFile>
    {
        public SyntaxTree? SourceTree { get; }
        public AdditionalText? NonSourceFile { get; }

        public SourceOrNonSourceFile(SyntaxTree tree)
            => SourceTree = tree;

        public SourceOrNonSourceFile(AdditionalText file)
            => NonSourceFile = file;

        public bool Equals([AllowNull] SourceOrNonSourceFile? other)
            => other != null && SourceTree == other.SourceTree && NonSourceFile == other.NonSourceFile;

        public override bool Equals(object? obj)
            => Equals(obj as SourceOrNonSourceFile);

        public static bool operator ==([AllowNull] SourceOrNonSourceFile? left, [AllowNull] SourceOrNonSourceFile? right)
            => Equals(left, right);

        public static bool operator !=([AllowNull] SourceOrNonSourceFile? left, [AllowNull] SourceOrNonSourceFile? right)
            => !Equals(left, right);

        public override int GetHashCode()
        {
            if (SourceTree != null)
            {
                return Hash.Combine(true, SourceTree.GetHashCode());
            }
            else
            {
                RoslynDebug.Assert(NonSourceFile != null);
                return Hash.Combine(false, NonSourceFile.GetHashCode());
            }
        }
    }
}
