// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal struct ActiveStatementSpan
    {
        public readonly ActiveStatementFlags Flags;
        public readonly LinePositionSpan Span;

        public ActiveStatementSpan(ActiveStatementFlags flags, LinePositionSpan span)
        {
            this.Flags = flags;
            this.Span = span;
        }

        public bool IsLeaf
        {
            get
            {
                return (Flags & ActiveStatementFlags.LeafFrame) != 0;
            }
        }
    }
}
