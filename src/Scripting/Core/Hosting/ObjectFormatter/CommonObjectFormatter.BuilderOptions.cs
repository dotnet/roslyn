// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class CommonObjectFormatter
    {
        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal struct BuilderOptions
        {
            public readonly string Indentation;
            public readonly string NewLine;
            public readonly string Ellipsis;

            public readonly int LineLengthLimit;
            public readonly int TotalLengthLimit;

            public BuilderOptions(string indentation, string newLine, string ellipsis, int lineLengthLimit, int totalLengthLimit)
            {
                Indentation = indentation;
                NewLine = newLine;
                Ellipsis = ellipsis;
                LineLengthLimit = lineLengthLimit;
                TotalLengthLimit = totalLengthLimit;
            }

            public BuilderOptions WithTotalLengthLimit(int totalLengthLimit)
            {
                return new BuilderOptions(
                    Indentation,
                    NewLine,
                    Ellipsis,
                    LineLengthLimit,
                    totalLengthLimit);
            }

            public BuilderOptions SubtractEllipsisLength()
            {
                return new BuilderOptions(
                    Indentation,
                    NewLine,
                    Ellipsis,
                    Math.Max(0, LineLengthLimit - Ellipsis.Length - 1),
                    Math.Max(0, TotalLengthLimit - Ellipsis.Length - 1));
            }
        }
    }
}