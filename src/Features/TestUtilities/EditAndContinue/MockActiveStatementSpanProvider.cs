// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockActiveStatementSpanProvider : IActiveStatementSpanFactory
    {
        public Func<Solution, ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>? GetBaseActiveStatementSpansImpl;
        public Func<TextDocument, ActiveStatementSpanProvider, ImmutableArray<ActiveStatementSpan>>? GetAdjustedActiveStatementSpansImpl;

        public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
            => new((GetBaseActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(solution, documentIds));

        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetAdjustedActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));
    }
}
