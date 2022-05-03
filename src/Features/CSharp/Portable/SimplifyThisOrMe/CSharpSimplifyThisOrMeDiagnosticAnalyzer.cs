// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.SimplifyThisOrMe;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyThisOrMeDiagnosticAnalyzer
        : AbstractSimplifyThisOrMeDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ThisExpressionSyntax,
            MemberAccessExpressionSyntax,
            CSharpSimplifierOptions>
    {
        protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

        protected override CSharpSimplifierOptions GetSimplifierOptions(AnalyzerOptions options, SyntaxTree syntaxTree)
            => options.GetCSharpSimplifierOptions(syntaxTree);

        protected override AbstractMemberAccessExpressionSimplifier<ExpressionSyntax, MemberAccessExpressionSyntax, ThisExpressionSyntax> Simplifier
            => MemberAccessExpressionSimplifier.Instance;
    }
}
