// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Roslyn.Utilities;

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

            public override bool IsExtractMethodOnMultipleStatements()
                => throw ExceptionUtilities.Unreachable();

            public override bool IsExtractMethodOnSingleStatement()
                => throw ExceptionUtilities.Unreachable();

            protected override bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken)
                => throw ExceptionUtilities.Unreachable();

            public override SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable();

            public override bool ContainingScopeHasAsyncKeyword()
                => throw ExceptionUtilities.Unreachable();

            public override SyntaxNode GetContainingScope()
                => throw ExceptionUtilities.Unreachable();

            public override ITypeSymbol GetContainingScopeType()
                => throw ExceptionUtilities.Unreachable();
        }

        protected class ErrorSelectionResult(OperationStatus status) : NullSelectionResult(status.MakeFail())
        {
        }
    }
}
