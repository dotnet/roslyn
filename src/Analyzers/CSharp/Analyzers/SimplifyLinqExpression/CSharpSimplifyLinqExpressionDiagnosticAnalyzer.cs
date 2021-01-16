// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionDiagnosticAnalyzer : AbstractSimplifyLinqExpressionDiagnosticAnalyzer<InvocationExpressionSyntax, MemberAccessExpressionSyntax>
    {
        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override IInvocationOperation? TryGetNextInvocationInChain(IInvocationOperation invocation)
            => invocation.Parent is IArgumentOperation argument &&
               argument.Parent is IInvocationOperation nextInvocation
                    ? nextInvocation
                    : null;
    }
}
