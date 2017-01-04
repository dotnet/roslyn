// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForAccessorsDiagnosticAnalyzer : 
        AbstractUseExpressionBodyDiagnosticAnalyzer<AccessorDeclarationSyntax>, IBuiltInAnalyzer
    {
        private readonly UseExpressionBodyForPropertiesDiagnosticAnalyzer propertyAnalyzer = new UseExpressionBodyForPropertiesDiagnosticAnalyzer();
        private readonly UseExpressionBodyForIndexersDiagnosticAnalyzer indexerAnalyzer = new UseExpressionBodyForIndexersDiagnosticAnalyzer();

        public UseExpressionBodyForAccessorsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   ImmutableArray.Create(SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration),
                   CSharpCodeStyleOptions.PreferExpressionBodiedAccessors)
        {
        }

        protected override BlockSyntax GetBody(AccessorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(AccessorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        internal override Diagnostic AnalyzeSyntax(OptionSet optionSet, AccessorDeclarationSyntax accessor)
        {
            // We don't want to double report.  So don't report a diagnostic if the property/indexer
            // analyzer is going to report a diagnostic here.
            var grandParent = accessor.Parent.Parent;

            if (grandParent.IsKind(SyntaxKind.PropertyDeclaration))
            {
                var propertyDeclaration = (PropertyDeclarationSyntax)grandParent;
                var diagnostic = propertyAnalyzer.AnalyzeSyntax(optionSet, propertyDeclaration);
                if (diagnostic != null)
                {
                    return null;
                }
            }
            else if (grandParent.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexerDeclaration = (IndexerDeclarationSyntax)grandParent;
                var diagnostic = indexerAnalyzer.AnalyzeSyntax(optionSet, indexerDeclaration);
                if (diagnostic != null)
                {
                    return null;
                }
            }

            return base.AnalyzeSyntax(optionSet, accessor);
        }
    }
}