// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseThrowExpression;

#if CODE_STYLE
using Microsoft.CodeAnalysis.CSharp.Internal.CodeStyle;
#else
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
#endif

namespace Microsoft.CodeAnalysis.CSharp.UseThrowExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseThrowExpressionDiagnosticAnalyzer : AbstractUseThrowExpressionDiagnosticAnalyzer
    {
        public CSharpUseThrowExpressionDiagnosticAnalyzer()
            : base(CSharpCodeStyleOptions.PreferThrowExpression, LanguageNames.CSharp)
        {
        }

        protected override bool IsSupported(ParseOptions options)
        {
            var csOptions = (CSharpParseOptions)options;
            return csOptions.LanguageVersion >= LanguageVersion.CSharp7;
        }

        protected override bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken)
            => node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken);
    }
}
