// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast;

/// <summary>
/// Supports simplifying cast expressions like <c>(T)x</c> as well as try-cast expressions like <c>x as T</c>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryCastDiagnosticAnalyzer
    : AbstractRemoveUnnecessaryCastDiagnosticAnalyzer<SyntaxKind, ExpressionSyntax>
{
    protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } =
        [SyntaxKind.CastExpression, SyntaxKind.AsExpression];

    protected override bool IsUnnecessaryCast(SemanticModel model, ExpressionSyntax cast, CancellationToken cancellationToken)
        => CastSimplifier.IsUnnecessaryCast(cast, model, cancellationToken);

    protected override TextSpan GetFadeSpan(ExpressionSyntax node)
        => node switch
        {
            CastExpressionSyntax cast => TextSpan.FromBounds(cast.OpenParenToken.SpanStart, cast.CloseParenToken.Span.End),
            BinaryExpressionSyntax binary => TextSpan.FromBounds(binary.OperatorToken.SpanStart, node.Span.End),
            _ => throw ExceptionUtilities.Unreachable(),
        };
}
