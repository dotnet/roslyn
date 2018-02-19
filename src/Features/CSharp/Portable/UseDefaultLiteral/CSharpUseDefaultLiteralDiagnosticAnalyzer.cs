﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseDefaultLiteralDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseDefaultLiteralDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_default_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.default_expression_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
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
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            var defaultExpression = (DefaultExpressionSyntax)context.Node;
            if (!defaultExpression.CanReplaceWithDefaultLiteral(parseOptions, optionSet, context.SemanticModel, cancellationToken))
            {
                return;
            }

            var fadeSpan = TextSpan.FromBounds(defaultExpression.OpenParenToken.SpanStart, defaultExpression.CloseParenToken.Span.End);

            // Create a normal diagnostic that covers the entire default expression.
            context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression).Notification.Value),
                    defaultExpression.GetLocation()));

            // Also fade out the part of the default expression from the open paren through 
            // the close paren.
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }
    }
}
