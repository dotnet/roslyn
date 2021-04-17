// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a line mapping defined by a single line mapping directive (<c>#line</c> in C# or <c>#ExternalSource</c> in VB).
    /// </summary>
    [DataContract]
    public readonly struct LineMapping : IEquatable<LineMapping>
    {
        /// <summary>
        /// The span in the syntax tree containing the line mapping directive.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly LinePositionSpan Span { get; }

        /// <summary>
        /// If the line mapping directive maps the span into an explicitly specified file the <see cref="FileLinePositionSpan.HasMappedPath"/> is true.
        /// If the path is not mapped <see cref="FileLinePositionSpan.Path"/> is empty and <see cref="FileLinePositionSpan.HasMappedPath"/> is false.
        /// If the line mapping directive marks hidden code <see cref="FileLinePositionSpan.IsValid"/> is false.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly FileLinePositionSpan MappedSpan { get; }

        public LineMapping(LinePositionSpan span, FileLinePositionSpan mappedSpan)
        {
            Span = span;
            MappedSpan = mappedSpan;
        }

        public void Deconstruct(out LinePositionSpan span, out FileLinePositionSpan mappedSpan)
        {
            span = Span;
            mappedSpan = MappedSpan;
        }

        /// <summary>
        /// True if the line mapping marks hidden code.
        /// </summary>
        public bool IsHidden
            => !MappedSpan.IsValid;

        public override bool Equals(object? obj)
            => obj is LineMapping other && Equals(other);

        public bool Equals(LineMapping other)
            => Span.Equals(other.Span) && MappedSpan.Equals(other.MappedSpan);

        public override int GetHashCode()
            => Hash.Combine(Span.GetHashCode(), MappedSpan.GetHashCode());

        public static bool operator ==(LineMapping left, LineMapping right)
            => left.Equals(right);

        public static bool operator !=(LineMapping left, LineMapping right)
            => !(left == right);

        public override string? ToString()
            => $"{Span} -> {MappedSpan}";
    }
}
