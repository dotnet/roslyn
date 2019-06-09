// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        public enum ResultKind
        {
            // like s.Substring(expr, s.Length - expr).  'expr' has to match on both sides.
            Computed,

            // like s.Substring(constant1, s.Length - constant2).  the constants don't have to match.
            Constant,
        }

        public readonly struct Result
        {
            public readonly ResultKind Kind;
            public readonly CodeStyleOption<bool> Option;
            public readonly IInvocationOperation InvocationOperation;
            public readonly InvocationExpressionSyntax Invocation;
            public readonly IMethodSymbol SliceLikeMethod;
            public readonly MemberInfo MemberInfo;
            public readonly IOperation Op1;
            public readonly IOperation Op2;

            public Result(
                ResultKind kind, CodeStyleOption<bool> option,
                IInvocationOperation invocationOperation, InvocationExpressionSyntax invocation,
                IMethodSymbol sliceLikeMethod, MemberInfo memberInfo,
                IOperation op1, IOperation op2)
            {
                Kind = kind;
                Option = option;
                InvocationOperation = invocationOperation;
                Invocation = invocation;
                SliceLikeMethod = sliceLikeMethod;
                MemberInfo = memberInfo;
                Op1 = op1;
                Op2 = op2;
            }
        }
    }
}
