// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseDefaultLiteralDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseDefaultLiteralDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId,
                   CSharpCodeStyleOptions.PreferSimpleDefaultExpression,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Simplify_default_expression), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.default_expression_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.DefaultExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = context.Node.SyntaxTree;
            var preference = context.Options.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, syntaxTree, cancellationToken);

            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            var defaultExpression = (DefaultExpressionSyntax)context.Node;
            if (!defaultExpression.CanReplaceWithDefaultLiteral(parseOptions, preference.Value, context.SemanticModel, cancellationToken))
            {
                return;
            }

            var fadeSpan = TextSpan.FromBounds(defaultExpression.OpenParenToken.SpanStart, defaultExpression.CloseParenToken.Span.End);

            // Create a normal diagnostic that covers the entire default expression.
            context.ReportDiagnostic(
                DiagnosticHelper.CreateWithLocationTags(
                    Descriptor,
                    defaultExpression.GetLocation(),
                    preference.Notification.Severity,
                    additionalLocations: ImmutableArray<Location>.Empty,
                    additionalUnnecessaryLocations: ImmutableArray.Create(defaultExpression.SyntaxTree.GetLocation(fadeSpan))));
        }
    }
}
