// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpSimplifyLinqExpressionDiagnosticAnalyzer : AbstractSimplifyLinqExpressionDiagnosticAnalyzer<InvocationExpressionSyntax, MemberAccessExpressionSyntax>
{
    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    protected override bool ConflictsWithMemberByNameOnly => false;

    protected override IInvocationOperation? TryGetNextInvocationInChain(IInvocationOperation invocation)
        // In C#, extension methods contain the methods they are being called from in the `this` parameter 
        // So in the case of A().ExtensionB() to get to ExtensionB from A we do the following:
        => invocation.Parent is IArgumentOperation { Parent: IInvocationOperation nextInvocation } ? nextInvocation : null;
}
