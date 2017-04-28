// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract class AbstractUseExpressionBodyDiagnosticAnalyzer<TDeclaration> :
        AbstractCodeStyleDiagnosticAnalyzer
        where TDeclaration : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxKind> _syntaxKinds;

        public override bool OpenFileOnly(Workspace workspace) => false;

        private readonly AbstractUseExpressionBodyHelper<TDeclaration> _helper;

        protected AbstractUseExpressionBodyDiagnosticAnalyzer(
            string diagnosticId,
            ImmutableArray<SyntaxKind> syntaxKinds,
            AbstractUseExpressionBodyHelper<TDeclaration> helper)
            : base(diagnosticId, helper.UseExpressionBodyTitle)
        {
            _syntaxKinds = syntaxKinds;
            _helper = helper;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, _syntaxKinds);

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

            var diagnostic = AnalyzeSyntax(optionSet, (TDeclaration)context.Node);
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private Diagnostic AnalyzeSyntax(OptionSet optionSet, TDeclaration declaration)
        {
            var preferExpressionBodiedOption = optionSet.GetOption(_helper.Option);
            var severity = preferExpressionBodiedOption.Notification.Value;

            if (_helper.CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: true))
            {
                var location = severity == DiagnosticSeverity.Hidden
                    ? declaration.GetLocation()
                    : _helper.GetBody(declaration).Statements[0].GetLocation();

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return Diagnostic.Create(
                    CreateDescriptorWithTitle(_helper.UseExpressionBodyTitle, severity, GetCustomTags(severity)),
                    location, additionalLocations: additionalLocations);
            }

            if (_helper.CanOfferUseBlockBody(optionSet, declaration, forAnalyzer: true))
            {
                // They have an expression body.  Create a diagnostic to conver it to a block
                // if they don't want expression bodies for this member.  
                var location = severity == DiagnosticSeverity.Hidden
                    ? declaration.GetLocation()
                    : _helper.GetExpressionBody(declaration).GetLocation();

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return Diagnostic.Create(
                    CreateDescriptorWithTitle(_helper.UseBlockBodyTitle, severity, GetCustomTags(severity)),
                    location, additionalLocations: additionalLocations);
            }

            return null;
        }

        private static string[] GetCustomTags(DiagnosticSeverity severity)
            => severity == DiagnosticSeverity.Hidden
                ? new[] { WellKnownDiagnosticTags.NotConfigurable }
                : Array.Empty<string>();
    }
}