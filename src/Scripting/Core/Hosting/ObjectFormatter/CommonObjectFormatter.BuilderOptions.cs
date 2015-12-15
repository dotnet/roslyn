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