// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpRemoveUnnecessaryCastDiagnosticAnalyzer
        : RemoveUnnecessaryCastDiagnosticAnalyzerBase<SyntaxKind, CastExpressionSyntax>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.CastExpression);

        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => s_kindsOfInterest;

        protected override bool IsUnnecessaryCast(SemanticModel model, CastExpressionSyntax cast, CancellationToken cancellationToken)
            => cast.IsUnnecessaryCast(model, cancellationToken);

        protected override TextSpan GetFadeSpan(CastExpressionSyntax node)
            => TextSpan.FromBounds(node.OpenParenToken.SpanStart, node.CloseParenToken.Span.End);
    }
}
