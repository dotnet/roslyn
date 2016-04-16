// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal struct ActiveStatementTextSpan
    {
        public readonly ActiveStatementFlags Flags;
        public readonly TextSpan Span;

        public ActiveStatementTextSpan(ActiveStatementFlags flags, TextSpan span)
        {
            this.Flags = flags;
            this.Span = span;
        }
    }
}
