// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SimplifyThisOrMe;
using Microsoft.CodeAnalysis.Text;

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
        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;

        protected override bool CanSimplifyTypeNameExpression(
            SemanticModel model, MemberAccessExpressionSyntax node, OptionSet optionSet,
            out TextSpan issueSpan, CancellationToken cancellationToken)
        {
            return ExpressionSimplifier.Instance.TrySimplify(node, model, optionSet, out _, out issueSpan, cancellationToken);
        }
    }
}
