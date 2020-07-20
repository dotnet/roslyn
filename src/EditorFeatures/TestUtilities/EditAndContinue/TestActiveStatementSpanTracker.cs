// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class TestActiveStatementSpanTracker : IActiveStatementSpanTracker
    {
        public Dictionary<DocumentId, TextSpan?[]>? Spans;

        public TestActiveStatementSpanTracker(Dictionary<DocumentId, TextSpan?[]>? spans = null)
        {
            Spans = spans;
        }

        public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
        {
            if (Spans == null)
            {
                span = default;
                return false;
            }

            var spans = Spans[id.DocumentId][id.Ordinal];
            if (spans != null)
            {
                span = spans.Value;
                return true;
            }

            span = default;
            return false;
        }
    }
}
