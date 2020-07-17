// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        public readonly struct Result
        {
            public readonly ResultKind Kind;
            public readonly CodeStyleOption2<bool> Option;
            public readonly IInvocationOperation InvocationOperation;
            public readonly InvocationExpressionSyntax Invocation;
            public readonly IMethodSymbol SliceLikeMethod;
            public readonly MemberInfo MemberInfo;
            public readonly IOperation Op1;

            /// <summary>
            /// Can be null, if we are dealing with one-argument call to a slice-like method.
            /// </summary>
            public readonly IOperation? Op2;

            public Result(
                ResultKind kind, CodeStyleOption2<bool> option,
                IInvocationOperation invocationOperation, InvocationExpressionSyntax invocation,
                IMethodSymbol sliceLikeMethod, MemberInfo memberInfo,
                IOperation op1, IOperation? op2)
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
