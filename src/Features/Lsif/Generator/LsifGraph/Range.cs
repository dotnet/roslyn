// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    internal sealed class Range : Vertex
    {
        public Position Start { get; }
        public Position End { get; }

        public Range(Position start, Position end)
            : base(label: "range")
        {
            Start = start;
            End = end;
        }

        public static Range FromTextSpan(TextSpan textSpan, SourceText sourceText)
        {
            var linePositionSpan = sourceText.Lines.GetLinePositionSpan(textSpan);

            return new Range(start: FromLinePositionSpan(linePositionSpan.Start), end: FromLinePositionSpan(linePositionSpan.End));
        }

        private static Position FromLinePositionSpan(LinePosition linePosition)
        {
            return new Position { Line = linePosition.Line, Character = linePosition.Character };
        }
    }
}
