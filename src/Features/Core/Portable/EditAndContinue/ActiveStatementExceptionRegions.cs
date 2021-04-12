﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
