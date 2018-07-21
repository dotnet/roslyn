// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForLambdaDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public const string FixesError = nameof(FixesError);

        public override bool OpenFileOnly(Workspace workspace) => false;

        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers =
            ImmutableArray.Create(UseExpressionBodyHelper.Instance);

        public UseExpressionBodyForLambdaDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId, _helpers[0].UseExpressionBodyTitle)
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

            var semanticModel = context.SemanticModel;

            var declaration = (LambdaExpressionSyntax)context.Node;
            var nodeKind = context.Node.Kind();
            foreach (var helper in _helpers)
            {
                var diagnostic = AnalyzeSyntax(
                    semanticModel, optionSet, declaration, helper, cancellationToken);
                if (diagnostic != null)
                {
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }

        private Diagnostic AnalyzeSyntax(
            SemanticModel semanticModel, OptionSet optionSet, LambdaExpressionSyntax declaration, 
            UseExpressionBodyHelper helper, CancellationToken cancellationToken)
        {
            var preferExpressionBodiedOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdaExpressions);
            var severity = preferExpressionBodiedOption.Notification.Severity;

            if (helper.CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: true))
            {
                var location = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                    ? declaration.GetLocation()
                    : helper.GetDiagnosticLocation(declaration);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                var properties = ImmutableDictionary<string, string>.Empty.Add(nameof(UseExpressionBody), "");
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(DescriptorId, helper.UseExpressionBodyTitle, helper.UseExpressionBodyTitle),
                    location, severity, additionalLocations: additionalLocations, properties: properties);
            }

            var (canOffer, fixesError) = helper.CanOfferUseBlockBody(
                semanticModel, optionSet, declaration, forAnalyzer: true, cancellationToken);
            if (canOffer)
            {
                // They have an expression body.  Create a diagnostic to convert it to a block
                // if they don't want expression bodies for this member.  
                var location = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                    ? declaration.GetLocation()
                    : helper.GetExpressionBody(declaration).GetLocation();

                var properties = ImmutableDictionary<string, string>.Empty;
                if (fixesError)
                {
                    properties = properties.Add(FixesError, "");
                }

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(DescriptorId, helper.UseBlockBodyTitle, helper.UseBlockBodyTitle),
                    location, severity, additionalLocations: additionalLocations, properties: properties);
            }

            return null;
        }
    }
}
