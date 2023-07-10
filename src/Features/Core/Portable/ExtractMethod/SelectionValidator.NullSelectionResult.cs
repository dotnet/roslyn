// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal partial class SelectionValidator
    {
        // null object
        protected class NullSelectionResult : SelectionResult
        {
            public NullSelectionResult()
                : this(OperationStatus.FailedWithUnknownReason)
            {
            }

            protected NullSelectionResult(OperationStatus status)
                : base(status)
            {
            }

            protected override bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken)
                => throw new InvalidOperationException();

            public override bool ContainingScopeHasAsyncKeyword()
                => throw new InvalidOperationException();

            public override SyntaxNode GetContainingScope()
                => throw new InvalidOperationException();

            public override ITypeSymbol GetContainingScopeType()
                => throw new InvalidOperationException();
        }

        protected class ErrorSelectionResult(OperationStatus status) : NullSelectionResult(status.MakeFail())
        {
        }
    }
}
