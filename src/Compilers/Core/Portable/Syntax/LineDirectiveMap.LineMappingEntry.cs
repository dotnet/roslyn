// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal partial class LineDirectiveMap<TDirective>
    {
        /// <summary>
        /// Enum that describes the state related to the #line or #externalsource directives at a position in source.
        /// </summary>
        public enum PositionState : byte
        {
            /// <summary>
            /// Used in VB when the position is not hidden, but it's not known yet that there is a (nonempty) <c>#ExternalSource</c>
            /// following.
            /// </summary>
            Unknown,

            /// <summary>
            /// Used in C# for spans preceding the first <c>#line</c> directive (if any) and for <c>#line default</c> spans
            /// </summary>
            Unmapped,

            /// <summary>
            /// Used in C# for spans inside of <c>#line linenumber</c> directive
            /// </summary>
            Remapped,

            /// <summary>
            /// Used in C# for spans inside of <c>#line (startLine, startChar) - (endLine, endChar) charOffset</c> directive
            /// </summary>
            RemappedSpan,

            /// <summary>
            /// Used in VB for spans inside of a <c>#ExternalSource</c> directive that followed an unknown span
            /// </summary>
            RemappedAfterUnknown,

            /// <summary>
            /// Used in VB for spans inside of a <c>#ExternalSource</c> directive that followed a hidden span
            /// </summary>
            RemappedAfterHidden,

            /// <summary>
            /// Used in C# and VB for spans that are inside of <c>#line hidden</c> (C#) or outside of <c>#ExternalSource</c> (VB) 
            /// directives
            /// </summary>
            Hidden
        }

        // Struct that represents an entry in the line mapping table. Entries sort by the unmapped
        // line.
        internal readonly struct LineMappingEntry : IComparable<LineMappingEntry>
        {
            // 0-based line in this tree
            public readonly int UnmappedLine;

            // 0-based line it maps to.
            public readonly int MappedLine;

            // 0-based mapped span from enhanced #line directive.
            public readonly LinePositionSpan MappedSpan;

            // optional character offset from enhanced #line directive.
            public readonly int? UnmappedCharacterOffset;

            // raw value from #line or #ExternalDirective, may be null
            public readonly string? MappedPathOpt;

            // the state of this line
            public readonly PositionState State;

            public LineMappingEntry(int unmappedLine)
            {
                this.UnmappedLine = unmappedLine;
                this.MappedLine = unmappedLine;
                this.MappedSpan = default;
                this.UnmappedCharacterOffset = null;
                this.MappedPathOpt = null;
                this.State = PositionState.Unmapped;
            }

            public LineMappingEntry(
                int unmappedLine,
                int mappedLine,
                string? mappedPathOpt,
                PositionState state)
            {
                Debug.Assert(state != PositionState.RemappedSpan);

                this.UnmappedLine = unmappedLine;
                this.MappedLine = mappedLine;
                this.MappedSpan = default;
                this.UnmappedCharacterOffset = null;
                this.MappedPathOpt = mappedPathOpt;
                this.State = state;
            }

            public LineMappingEntry(
                int unmappedLine,
                LinePositionSpan mappedSpan,
                int? unmappedCharacterOffset,
                string? mappedPathOpt)
            {
                this.UnmappedLine = unmappedLine;
                this.MappedLine = -1;
                this.MappedSpan = mappedSpan;
                this.UnmappedCharacterOffset = unmappedCharacterOffset;
                this.MappedPathOpt = mappedPathOpt;
                this.State = PositionState.RemappedSpan;
            }

            public int CompareTo(LineMappingEntry other)
                => UnmappedLine.CompareTo(other.UnmappedLine);

            public bool IsHidden
                => State == PositionState.Hidden;
        }
    }
}
