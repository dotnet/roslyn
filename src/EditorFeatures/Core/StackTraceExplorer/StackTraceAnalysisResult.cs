// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal class StackTraceAnalysisResult
    {
        public StackTraceAnalysisResult(
            ImmutableArray<StackTraceLine> parsedLines)
        {
            ParsedLines = parsedLines;
        }

        public ImmutableArray<StackTraceLine> ParsedLines { get; }
    }
}
