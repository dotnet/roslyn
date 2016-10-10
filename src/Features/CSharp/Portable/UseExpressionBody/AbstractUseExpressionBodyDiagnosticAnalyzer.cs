// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract class AbstractUseExpressionBodyDiagnosticAnalyzer<TDeclaration> :
        AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
        where TDeclaration : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxKind> _syntaxKinds;
        private readonly Option<CodeStyleOption<bool>> _option;
        private readonly LocalizableString _expressionBodyTitle;
        private readonly LocalizableString _blockBodyTitle;

        public bool OpenFileOnly(Workspace workspace) => true;

        protected AbstractUseExpressionBodyDiagnosticAnalyzer(
            string diagnosticId,
            LocalizableString expressionBodyTitle,
            LocalizableString blockBodyTitle,
            ImmutableArray<SyntaxKind> syntaxKinds,
            Option<CodeStyleOption<bool>> option)
            : base(diagnosticId, expressionBodyTitle)
        {
            _syntaxKinds = syntaxKinds;
            _option = option;
            _expressionBodyTitle = expressionBodyTitle;
            _blockBodyTitle = blockBodyTitle;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, _syntaxKinds);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var optionSet = context.Options.GetOptionSet();
            var preferExpressionBodiedOption = optionSet.GetOption(_option);

            var declaration = (TDeclaration)context.Node;
            var expressionBody = GetExpressionBody(declaration);

            if (preferExpressionBodiedOption.Value)
            {
                if (expressionBody == null)
                {
                    // They want expression bodies and they don't have one.  See if we can
                    // convert this to have an expression body.
                    expressionBody = GetBody(declaration).TryConvertToExpressionBody();
                    if (expressionBody != null)
                    {
                        var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                        context.ReportDiagnostic(Diagnostic.Create(
                            CreateDescriptor(this.DescriptorId, _expressionBodyTitle, preferExpressionBodiedOption.Notification.Value),
                            GetBody(declaration).Statements[0].GetLocation(),
                            additionalLocations: additionalLocations));
                    }
                }
            }
            else
            {
                // They don't want expression bodies but they have one.  Offer to conver this to a normal block
                if (expressionBody != null)
                {
                    var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                    context.ReportDiagnostic(Diagnostic.Create(
                        CreateDescriptor(this.DescriptorId, _blockBodyTitle, preferExpressionBodiedOption.Notification.Value),
                        expressionBody.GetLocation(),
                        additionalLocations: additionalLocations));
                }
            }
        }

        protected abstract BlockSyntax GetBody(TDeclaration declaration);
        protected abstract ArrowExpressionClauseSyntax GetExpressionBody(TDeclaration declaration);
    }
}