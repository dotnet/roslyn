// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    // Code for the DiagnosticAnalyzer ("Analysis") portion of the feature.

    internal partial class UseExpressionBodyForLambdaCodeStyleProvider
    {
        protected override void DiagnosticAnalyzerInitialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax,
                SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);

        protected override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, CodeStyleOption2<ExpressionBodyPreference> option)
        {
            var declaration = (LambdaExpressionSyntax)context.Node;
            var diagnostic = AnalyzeSyntax(context.SemanticModel, option, declaration, context.CancellationToken);
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private Diagnostic AnalyzeSyntax(
            SemanticModel semanticModel, CodeStyleOption2<ExpressionBodyPreference> option,
            LambdaExpressionSyntax declaration, CancellationToken cancellationToken)
        {
            if (CanOfferUseExpressionBody(option.Value, declaration))
            {
                var location = GetDiagnosticLocation(declaration);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                var properties = ImmutableDictionary<string, string>.Empty;
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(UseExpressionBodyTitle, UseExpressionBodyTitle),
                    location, option.Notification.Severity, additionalLocations, properties);
            }

            if (CanOfferUseBlockBody(semanticModel, option.Value, declaration, cancellationToken))
            {
                // They have an expression body.  Create a diagnostic to convert it to a block
                // if they don't want expression bodies for this member.  
                var location = GetDiagnosticLocation(declaration);

                var properties = ImmutableDictionary<string, string>.Empty;
                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(UseBlockBodyTitle, UseBlockBodyTitle),
                    location, option.Notification.Severity, additionalLocations, properties);
            }

            return null;
        }

        private static Location GetDiagnosticLocation(LambdaExpressionSyntax declaration)
            => Location.Create(declaration.SyntaxTree,
                    TextSpan.FromBounds(declaration.SpanStart, declaration.ArrowToken.Span.End));
    }
}
