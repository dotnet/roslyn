// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SimplifyBooleanExpression;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyBooleanExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpSimplifyConditionalDiagnosticAnalyzer :
    AbstractSimplifyConditionalDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        ConditionalExpressionSyntax>
{
    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    protected override CommonConversion GetConversion(SemanticModel semanticModel, ExpressionSyntax node, CancellationToken cancellationToken)
        => semanticModel.GetConversion(node, cancellationToken).ToCommonConversion();
}
