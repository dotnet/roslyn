// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct DocumentActiveStatementChanges
    {
        public readonly ImmutableArray<UnmappedActiveStatement> OldStatements;
        public readonly ImmutableArray<ActiveStatement> NewStatements;
        public readonly ImmutableArray<ImmutableArray<SourceFileSpan>> NewExceptionRegions;

        public DocumentActiveStatementChanges(
            ImmutableArray<UnmappedActiveStatement> oldSpans,
            ImmutableArray<ActiveStatement> newStatements,
            ImmutableArray<ImmutableArray<SourceFileSpan>> newExceptionRegions)
        {
            Contract.ThrowIfFalse(oldSpans.Length == newStatements.Length);
            Contract.ThrowIfFalse(oldSpans.Length == newExceptionRegions.Length);

#if DEBUG
            for (var i = 0; i < oldSpans.Length; i++)
            {
                // old and new exception region counts must match:
                Debug.Assert(oldSpans[i].ExceptionRegions.Spans.Length == newExceptionRegions[i].Length);
            }
#endif

            OldStatements = oldSpans;
            NewStatements = newStatements;
            NewExceptionRegions = newExceptionRegions;
        }

        public void Deconstruct(
            out ImmutableArray<UnmappedActiveStatement> oldStatements,
            out ImmutableArray<ActiveStatement> newStatements,
            out ImmutableArray<ImmutableArray<SourceFileSpan>> newExceptionRegions)
        {
            oldStatements = OldStatements;
            newStatements = NewStatements;
            newExceptionRegions = NewExceptionRegions;
        }
    }
}
