// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct ActiveStatementExceptionRegions
{
    /// <summary>
    /// Exception region spans corresponding to an active statement.
    /// </summary>
    public readonly ImmutableArray<SourceFileSpan> Spans;

    /// <summary>
    /// True if the active statement is covered by any of the exception region spans.
    /// </summary>
    public readonly bool IsActiveStatementCovered;

    public ActiveStatementExceptionRegions(ImmutableArray<SourceFileSpan> spans, bool isActiveStatementCovered)
    {
        Contract.ThrowIfTrue(spans.IsDefault);

        Spans = spans;
        IsActiveStatementCovered = isActiveStatementCovered;
    }
}
