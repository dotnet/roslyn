// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    using static UseExpressionBodyForLambdaHelpers;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class UseExpressionBodyForLambdaDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string FixesError = nameof(FixesError);

        public UseExpressionBodyForLambdaDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                   LanguageNames.CSharp,
                   UseExpressionBodyTitle)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(
                AnalyzeSyntax,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var declaration = (LambdaExpressionSyntax)context.Node;
            var diagnostic = AnalyzeSyntax(
                context.SemanticModel, optionSet, declaration, cancellationToken);
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private Diagnostic AnalyzeSyntax(
            SemanticModel semanticModel, OptionSet optionSet,
            LambdaExpressionSyntax declaration, CancellationToken cancellationToken)
        {
            var preferExpressionBodiedOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas);
            var severity = preferExpressionBodiedOption.Notification.Severity;

            if (CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: true))
            {
                var location = GetDiagnosticLocation(declaration);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                var properties = ImmutableDictionary<string, string>.Empty.Add(nameof(UseExpressionBody), "");
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(DescriptorId, UseExpressionBodyTitle, UseExpressionBodyTitle),
                    location, severity, additionalLocations, properties);
            }

            var (canOffer, fixesError) = CanOfferUseBlockBody(
                semanticModel, optionSet, declaration, forAnalyzer: true, cancellationToken);
            if (canOffer)
            {
                // They have an expression body.  Create a diagnostic to convert it to a block
                // if they don't want expression bodies for this member.  
                var location = GetDiagnosticLocation(declaration);

                var properties = ImmutableDictionary<string, string>.Empty;
                if (fixesError)
                {
                    properties = properties.Add(FixesError, "");
                }

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(DescriptorId, UseBlockBodyTitle, UseBlockBodyTitle),
                    location, severity, additionalLocations, properties);
            }

            return null;
        }

        private static Location GetDiagnosticLocation(LambdaExpressionSyntax declaration)
            => Location.Create(declaration.SyntaxTree,
                    TextSpan.FromBounds(declaration.SpanStart, declaration.ArrowToken.Span.End));
    }
}
