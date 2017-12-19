// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal struct ActiveExceptionRegion
    {
        public readonly int ActiveStatementDebuggerId;

        /// <summary>
        /// The ordinal of the exception region in a sequence of exception regions that are associated with the active statement.
        /// </summary>
        public readonly int Ordinal;

        public readonly LinePositionSpan Span;

        public ActiveExceptionRegion(int activeStatementDebuggerId, int ordinal, LinePositionSpan span)
        {
            ActiveStatementDebuggerId = activeStatementDebuggerId;
            Span = span;
            Ordinal = ordinal;
        }
    }
}
