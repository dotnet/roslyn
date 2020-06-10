// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a source file or a non-source file.
    /// For source files, <see cref="SourceTree"/> is non-null and <see cref="NonSourceFile"/> is null.
    /// For non-source files, <see cref="NonSourceFile"/> is non-null and <see cref="SourceTree"/> is null.
    /// </summary>
    internal abstract class SourceOrNonSourceFile
        : IEquatable<SourceOrNonSourceFile>
    {
        public abstract SyntaxTree? SourceTree { get; }
        public abstract AdditionalText? NonSourceFile { get; }
        public abstract bool Equals([AllowNull] SourceOrNonSourceFile? other);
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        public static SourceOrNonSourceFile Create(SyntaxTree tree)
        {
            return new SourceFileImpl(tree);
        }

        public static SourceOrNonSourceFile Create(AdditionalText nonSourceFile)
        {
            return new NonSourceFileImpl(nonSourceFile);
        }

        private sealed class SourceFileImpl : SourceOrNonSourceFile
        {
            public SourceFileImpl(SyntaxTree tree)
            {
                SourceTree = tree;
            }

            public override SyntaxTree? SourceTree { get; }
            public override AdditionalText? NonSourceFile => null;
            public override bool Equals(object? obj)
                => Equals(obj as SourceFileImpl);
            public override bool Equals([AllowNull] SourceOrNonSourceFile? other)
                => other is SourceFileImpl otherSource &&
                   SourceTree == otherSource.SourceTree;
            public override int GetHashCode()
                => SourceTree!.GetHashCode();
        }

        private sealed class NonSourceFileImpl : SourceOrNonSourceFile
        {
            public NonSourceFileImpl(AdditionalText nonSourceFile)
            {
                NonSourceFile = nonSourceFile;
            }

            public override AdditionalText? NonSourceFile { get; }
            public override SyntaxTree? SourceTree => null;
            public override bool Equals(object? obj)
                => Equals(obj as NonSourceFileImpl);
            public override bool Equals([AllowNull] SourceOrNonSourceFile? other)
                => other is NonSourceFileImpl otherNonSource &&
                   NonSourceFile == otherNonSource.NonSourceFile;
            public override int GetHashCode()
                => NonSourceFile!.GetHashCode();
        }
    }
}
