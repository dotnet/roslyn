// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementExceptionRegions
    {
        /// <summary>
        /// Exception region spans corresponding to an active statement.
        /// </summary>
        public readonly ImmutableArray<LinePositionSpan> Spans;

        /// <summary>
        /// True if the active statement is covered by any of the exception region spans.
        /// </summary>
        public readonly bool IsActiveStatementCovered;

        public ActiveStatementExceptionRegions(ImmutableArray<LinePositionSpan> spans, bool isActiveStatementCovered)
        {
            Spans = spans;
            IsActiveStatementCovered = isActiveStatementCovered;
        }
    }
}
