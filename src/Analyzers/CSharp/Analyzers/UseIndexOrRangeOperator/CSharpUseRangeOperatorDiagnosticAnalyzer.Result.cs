﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        public enum ResultKind
        {
            // like s.Substring(expr, s.Length - expr) or s.Substring(expr).  'expr' has to match on both sides.
            Computed,

            // like s.Substring(constant1, s.Length - constant2).  the constants don't have to match.
            Constant,
        }

        public readonly struct Result(
            ResultKind kind,
            IInvocationOperation invocationOperation,
            InvocationExpressionSyntax invocation,
            IMethodSymbol sliceLikeMethod,
            MemberInfo memberInfo,
            IOperation op1,
            IOperation? op2)
        {
            public readonly ResultKind Kind = kind;
            public readonly IInvocationOperation InvocationOperation = invocationOperation;
            public readonly InvocationExpressionSyntax Invocation = invocation;
            public readonly IMethodSymbol SliceLikeMethod = sliceLikeMethod;
            public readonly MemberInfo MemberInfo = memberInfo;
            public readonly IOperation Op1 = op1;

            /// <summary>
            /// Can be null, if we are dealing with one-argument call to a slice-like method.
            /// </summary>
            public readonly IOperation? Op2 = op2;
        }
    }
}
