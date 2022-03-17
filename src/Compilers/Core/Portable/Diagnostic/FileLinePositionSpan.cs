// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a span of text in a source code file in terms of file name, line number, and offset within line.
    /// However, the file is actually whatever was passed in when asked to parse; there may not really be a file.
    /// </summary>
    public readonly struct FileLinePositionSpan : IEquatable<FileLinePositionSpan>
    {
        /// <summary>
        /// Path, or null if the span represents an invalid value.
        /// </summary>
        /// <remarks>
        /// Path may be <see cref="string.Empty"/> if not available.
        /// </remarks>
        public string Path { get; }

        /// <summary>
        /// True if the <see cref="Path"/> is a mapped path.
        /// </summary>
        /// <remarks>
        /// A mapped path is a path specified in source via <c>#line</c> (C#) or <c>#ExternalSource</c> (VB) directives.
        /// </remarks>
        public bool HasMappedPath { get; }

        /// <summary>
        /// Gets the span.
        /// </summary>
        public LinePositionSpan Span { get; }

        /// <summary>
        /// Initializes the <see cref="FileLinePositionSpan"/> instance.
        /// </summary>
        /// <param name="path">The file identifier - typically a relative or absolute path.</param>
        /// <param name="start">The start line position.</param>
        /// <param name="end">The end line position.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public FileLinePositionSpan(string path, LinePosition start, LinePosition end)
            : this(path, new LinePositionSpan(start, end))
        {
        }

        /// <summary>
        /// Initializes the <see cref="FileLinePositionSpan"/> instance.
        /// </summary>
        /// <param name="path">The file identifier - typically a relative or absolute path.</param>
        /// <param name="span">The span.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public FileLinePositionSpan(string path, LinePositionSpan span)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Span = span;
            HasMappedPath = false;
        }

        internal FileLinePositionSpan(string path, LinePositionSpan span, bool hasMappedPath)
        {
            Path = path;
            Span = span;
            HasMappedPath = hasMappedPath;
        }

        /// <summary>
        /// Gets the <see cref="LinePosition"/> of the start of the span.
        /// </summary>
        /// <returns></returns>
        public LinePosition StartLinePosition => Span.Start;

        /// <summary>
        /// Gets the <see cref="LinePosition"/> of the end of the span.
        /// </summary>
        /// <returns></returns>
        public LinePosition EndLinePosition => Span.End;

        /// <summary>
        /// Returns true if the span represents a valid location.
        /// </summary>
        public bool IsValid
            => Path != null; // invalid span can be constructed by new FileLinePositionSpan()

        /// <summary>
        /// Determines if two FileLinePositionSpan objects are equal.
        /// </summary>
        /// <remarks>
        /// The path is treated as an opaque string, i.e. a case-sensitive comparison is used.
        /// </remarks>
        public bool Equals(FileLinePositionSpan other)
            => Span.Equals(other.Span) &&
               HasMappedPath == other.HasMappedPath &&
               string.Equals(Path, other.Path, StringComparison.Ordinal);

        /// <summary>
        /// Determines if two FileLinePositionSpan objects are equal.
        /// </summary>
        public override bool Equals(object? other)
            => other is FileLinePositionSpan span && Equals(span);

        /// <summary>
        /// Serves as a hash function for FileLinePositionSpan.
        /// </summary>
        /// <returns>The hash code.</returns>
        /// <remarks>
        /// The path is treated as an opaque string, i.e. a case-sensitive hash is calculated.
        /// </remarks>
        public override int GetHashCode()
            => Hash.Combine(Path, Hash.Combine(HasMappedPath, Span.GetHashCode()));

        /// <summary>
        /// Returns a <see cref="string"/> that represents <see cref="FileLinePositionSpan"/>.
        /// </summary>
        /// <returns>The string representation of <see cref="FileLinePositionSpan"/>.</returns>
        /// <example>Path: (0,0)-(5,6)</example>
        public override string ToString()
            => Path + ": " + Span;

        public static bool operator ==(FileLinePositionSpan left, FileLinePositionSpan right)
            => left.Equals(right);

        public static bool operator !=(FileLinePositionSpan left, FileLinePositionSpan right)
            => !(left == right);
    }
}
