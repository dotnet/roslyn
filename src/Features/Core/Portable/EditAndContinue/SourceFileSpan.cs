// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents a span of text in a source code file in terms of file name, line number, and offset within line.
    /// An alternative for <see cref="FileLinePositionSpan"/> without <see cref="FileLinePositionSpan.HasMappedPath"/> bit.
    /// </summary>
    /// <remarks>
    /// Initializes the <see cref="SourceFileSpan"/> instance.
    /// </remarks>
    /// <param name="path">The file identifier - typically a relative or absolute path.</param>
    /// <param name="span">The span.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    [DataContract]
    internal readonly struct SourceFileSpan(string path, LinePositionSpan span) : IEquatable<SourceFileSpan>
    {
        /// <summary>
        /// Path, or null if the span represents an invalid value.
        /// </summary>
        /// <remarks>
        /// Path may be <see cref="string.Empty"/> if not available.
        /// </remarks>
        [DataMember(Order = 0)]
        public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

        /// <summary>
        /// Gets the span.
        /// </summary>
        [DataMember(Order = 1)]
        public LinePositionSpan Span { get; } = span;

        public SourceFileSpan WithSpan(LinePositionSpan span)
            => new(Path, span);

        public SourceFileSpan WithPath(string path)
            => new(path, Span);

        /// <summary>
        /// Returns true if the span represents a valid location.
        /// </summary>
        public bool IsValid
            => Path != null; // invalid span can be constructed by new SourceFileSpan()

        /// <summary>
        /// Gets the <see cref="LinePosition"/> of the start of the span.
        /// </summary>
        public LinePosition Start
            => Span.Start;

        /// <summary>
        /// Gets the <see cref="LinePosition"/> of the end of the span.
        /// </summary>
        public LinePosition End
            => Span.End;

        public bool Equals(SourceFileSpan other)
            => Span.Equals(other.Span) && string.Equals(Path, other.Path, StringComparison.Ordinal);

        public override bool Equals(object? other)
            => other is SourceFileSpan span && Equals(span);

        public override int GetHashCode()
            => Hash.Combine(Path, Span.GetHashCode());

        public override string ToString()
            => string.IsNullOrEmpty(Path) ? Span.ToString() : $"{Path}: {Span}";

        public static implicit operator SourceFileSpan(FileLinePositionSpan span)
            => new(span.Path, span.Span);

        public static bool operator ==(SourceFileSpan left, SourceFileSpan right)
            => left.Equals(right);

        public static bool operator !=(SourceFileSpan left, SourceFileSpan right)
            => !(left == right);

        public bool Contains(SourceFileSpan span)
            => Span.Contains(span.Span) && string.Equals(Path, span.Path, StringComparison.Ordinal);
    }
}
