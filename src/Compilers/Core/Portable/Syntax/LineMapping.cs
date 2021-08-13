// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a line mapping defined by a single line mapping directive (<c>#line</c> in C# or <c>#ExternalSource</c> in VB).
    /// </summary>
    public readonly struct LineMapping : IEquatable<LineMapping>
    {
        /// <summary>
        /// The span in the syntax tree containing the line mapping directive.
        /// </summary>
        public readonly LinePositionSpan Span { get; }

        /// <summary>
        /// The optional offset in the syntax tree for the line immediately following an enhanced <c>#line</c> directive in C#.
        /// </summary>
        public readonly int? CharacterOffset { get; }

        /// <summary>
        /// If the line mapping directive maps the span into an explicitly specified file the <see cref="FileLinePositionSpan.HasMappedPath"/> is true.
        /// If the path is not mapped <see cref="FileLinePositionSpan.Path"/> is empty and <see cref="FileLinePositionSpan.HasMappedPath"/> is false.
        /// If the line mapping directive marks hidden code <see cref="FileLinePositionSpan.IsValid"/> is false.
        /// </summary>
        public readonly FileLinePositionSpan MappedSpan { get; }

        public LineMapping(LinePositionSpan span, int? characterOffset, FileLinePositionSpan mappedSpan)
        {
            Span = span;
            CharacterOffset = characterOffset;
            MappedSpan = mappedSpan;
        }

        /// <summary>
        /// True if the line mapping marks hidden code.
        /// </summary>
        public bool IsHidden
            => !MappedSpan.IsValid;

        public override bool Equals(object? obj)
            => obj is LineMapping other && Equals(other);

        public bool Equals(LineMapping other)
            => Span.Equals(other.Span) && CharacterOffset.Equals(other.CharacterOffset) && MappedSpan.Equals(other.MappedSpan);

        public override int GetHashCode()
            => Hash.Combine(Hash.Combine(Span.GetHashCode(), CharacterOffset.GetHashCode()), MappedSpan.GetHashCode());

        public static bool operator ==(LineMapping left, LineMapping right)
            => left.Equals(right);

        public static bool operator !=(LineMapping left, LineMapping right)
            => !(left == right);

        public override string? ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Span);
            if (CharacterOffset.HasValue)
            {
                builder.Append(",");
                builder.Append(CharacterOffset.GetValueOrDefault());
            }
            builder.Append(" -> ");
            builder.Append(MappedSpan);
            return builder.ToString();
        }
    }
}
