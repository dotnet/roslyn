﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a Range for serialization. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#ranges for further details.
    /// </summary>
    internal sealed class Range : Vertex
    {
        public Position Start { get; }
        public Position End { get; }

        public Range(Position start, Position end, IdFactory idFactory)
            : base(label: "range", idFactory)
        {
            Start = start;
            End = end;
        }

        public static Range FromTextSpan(TextSpan textSpan, SourceText sourceText, IdFactory idFactory)
        {
            var linePositionSpan = sourceText.Lines.GetLinePositionSpan(textSpan);

            return new Range(start: ConvertLinePositionToPosition(linePositionSpan.Start), end: ConvertLinePositionToPosition(linePositionSpan.End), idFactory);
        }

        internal static Position ConvertLinePositionToPosition(LinePosition linePosition)
        {
            return new Position { Line = linePosition.Line, Character = linePosition.Character };
        }
    }
}
