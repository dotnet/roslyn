// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseDefaultLiteralDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly LiteralExpressionSyntax s_defaultLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

        public CSharpUseDefaultLiteralDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_default_literal), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.DefaultExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var syntaxTree = context.Node.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_1)
            {
                return;
            }

            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var codeStyleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferDefaultLiteral);
            if (!codeStyleOption.Value)
            {
                return;
            }

            var defaultExpression = (DefaultExpressionSyntax)context.Node;
            if (!CanUseDefaultLiteral(defaultExpression, context.SemanticModel, cancellationToken))
            {
                return;
            }

            var fadeSpan = TextSpan.FromBounds(defaultExpression.OpenParenToken.SpanStart, defaultExpression.CloseParenToken.Span.End);

            context.ReportDiagnostic(Diagnostic.Create(GetDescriptorWithSeverity(codeStyleOption.Notification.Value), defaultExpression.GetLocation()));
            context.ReportDiagnostic(Diagnostic.Create(UnnecessaryWithoutSuggestionDescriptor, syntaxTree.GetLocation(fadeSpan)));
        }

        private bool CanUseDefaultLiteral(
            DefaultExpressionSyntax defaultExpression,
            SemanticModel semanticModel, 
            CancellationToken cancellationToken)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(
                defaultExpression, s_defaultLiteralExpression, semanticModel,
                cancellationToken,
                skipVerificationForReplacedNode: true,
                failOnOverloadResolutionFailuresInOriginalCode: true);

            return !speculationAnalyzer.ReplacementChangesSemantics();
        }
    }
}