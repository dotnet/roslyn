// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Simplification;
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
            MemberAccessExpressionSyntax>
    {
        protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

        protected override ISimplification Simplification
            => CSharpSimplification.Instance;

        protected override AbstractMemberAccessExpressionSimplifier<ExpressionSyntax, MemberAccessExpressionSyntax, ThisExpressionSyntax> Simplifier
            => MemberAccessExpressionSimplifier.Instance;
    }
}
