// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a span of text in a source code file in terms of file name, line number, and offset within line.
    /// However, the file is actually whatever was passed in when asked to parse; there may not really be a file.
    /// </summary>
    public struct FileLinePositionSpan : IEquatable<FileLinePositionSpan>
    {
        private readonly string _path;
        private readonly LinePositionSpan _span;
        private readonly bool _hasMappedPath;

        /// <summary>
        /// Path, or null if the span represents an invalid value.
        /// </summary>
        /// <remarks>
        /// Path may be <see cref="string.Empty"/> if not available.
        /// </remarks>
        public string Path { get { return _path; } }

        /// <summary>
        /// True if the <see cref="Path"/> is a mapped path.
        /// </summary>
        /// <remarks>
        /// A mapped path is a path specified in source via <c>#line</c> (C#) or <c>#ExternalSource</c> (VB) directives.
        /// </remarks>
        public bool HasMappedPath { get { return _hasMappedPath; } }

        /// <summary>
        /// Gets the <see cref="LinePosition"/> of the start of the span.
        /// </summary>
        /// <returns></returns>
        public LinePosition StartLinePosition { get { return _span.Start; } }

        /// <summary>
        /// Gets the <see cref="LinePosition"/> of the end of the span.
        /// </summary>
        /// <returns></returns>
        public LinePosition EndLinePosition { get { return _span.End; } }

        /// <summary>
        /// Gets the span.
        /// </summary>
        public LinePositionSpan Span
        {
            get
            {
                return _span;
            }
        }

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
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _path = path;
            _span = span;
            _hasMappedPath = false;
        }

        internal FileLinePositionSpan(string path, LinePositionSpan span, bool hasMappedPath)
        {
            _path = path;
            _span = span;
            _hasMappedPath = hasMappedPath;
        }

        /// <summary>
        /// Returns true if the span represents a valid location.
        /// </summary>
        public bool IsValid
        {
            get
            {
                // invalid span can be constructed by new FileLinePositionSpan()
                return _path != null;
            }
        }

        /// <summary>
        /// Determines if two FileLinePositionSpan objects are equal.
        /// </summary>
        /// <remarks>
        /// The path is treated as an opaque string, i.e. a case-sensitive comparison is used.
        /// </remarks>
        public bool Equals(FileLinePositionSpan other)
        {
            return _span.Equals(other._span)
                && _hasMappedPath == other._hasMappedPath
                && string.Equals(_path, other._path, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines if two FileLinePositionSpan objects are equal.
        /// </summary>
        public override bool Equals(object other)
        {
            return other is FileLinePositionSpan && Equals((FileLinePositionSpan)other);
        }

        /// <summary>
        /// Serves as a hash function for FileLinePositionSpan.
        /// </summary>
        /// <returns>The hash code.</returns>
        /// <remarks>
        /// The path is treated as an opaque string, i.e. a case-sensitive hash is calculated.
        /// </remarks>
        public override int GetHashCode()
        {
            return Hash.Combine(_path, Hash.Combine(_hasMappedPath, _span.GetHashCode()));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents FileLinePositionSpan.
        /// </summary>
        /// <returns>The string representation of FileLinePositionSpan.</returns>
        /// <example>Path: (0,0)-(5,6)</example>
        public override string ToString()
        {
            return _path + ": " + _span;
        }
    }
}
