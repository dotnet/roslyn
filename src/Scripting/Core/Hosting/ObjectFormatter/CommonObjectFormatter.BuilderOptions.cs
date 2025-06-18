// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    internal abstract partial class CommonObjectFormatter
    {
        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal readonly struct BuilderOptions
        {
            public readonly string Indentation;
            public readonly string NewLine;
            public readonly string Ellipsis;

            public readonly int MaximumLineLength;
            public readonly int MaximumOutputLength;

            public BuilderOptions(string indentation, string newLine, string ellipsis, int maximumLineLength, int maximumOutputLength)
            {
                Indentation = indentation;
                NewLine = newLine;
                Ellipsis = ellipsis;
                MaximumLineLength = maximumLineLength;
                MaximumOutputLength = maximumOutputLength;
            }

            public BuilderOptions WithMaximumOutputLength(int maximumOutputLength)
            {
                return new BuilderOptions(
                    Indentation,
                    NewLine,
                    Ellipsis,
                    MaximumLineLength,
                    maximumOutputLength);
            }
        }
    }
}
