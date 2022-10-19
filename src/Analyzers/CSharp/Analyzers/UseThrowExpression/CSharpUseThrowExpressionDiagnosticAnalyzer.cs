// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseThrowExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseThrowExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseThrowExpressionDiagnosticAnalyzer : AbstractUseThrowExpressionDiagnosticAnalyzer
    {
        public CSharpUseThrowExpressionDiagnosticAnalyzer()
            : base(CSharpCodeStyleOptions.PreferThrowExpression, LanguageNames.CSharp)
        {
        }

        protected override bool IsSupported(Compilation compilation)
            => compilation.LanguageVersion() >= LanguageVersion.CSharp7;

        protected override bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol? expressionTypeOpt, CancellationToken cancellationToken)
            => node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken);
    }
}
