// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal partial class SelectionValidator
    {
        // null object
        protected class NullSelectionResult : SelectionResult
        {
            public NullSelectionResult() :
                this(OperationStatus.FailedWithUnknownReason)
            {
            }

            protected NullSelectionResult(OperationStatus status) :
                base(status)
            {
            }

            protected override bool UnderAsyncAnonymousMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken)
            {
                throw new InvalidOperationException();
            }

            public override bool ContainingScopeHasAsyncKeyword()
            {
                throw new InvalidOperationException();
            }

            public override SyntaxNode GetContainingScope()
            {
                throw new InvalidOperationException();
            }

            public override ITypeSymbol GetContainingScopeType()
            {
                throw new InvalidOperationException();
            }
        }

        protected class ErrorSelectionResult : NullSelectionResult
        {
            public ErrorSelectionResult(OperationStatus status) :
                base(status.MakeFail())
            {
            }
        }
    }
}
