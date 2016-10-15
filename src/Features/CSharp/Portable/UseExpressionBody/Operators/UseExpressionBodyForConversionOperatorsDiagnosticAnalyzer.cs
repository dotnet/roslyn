// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForConversionOperatorsDiagnosticAnalyzer :
        AbstractUseExpressionBodyDiagnosticAnalyzer<ConversionOperatorDeclarationSyntax>, IBuiltInAnalyzer
    {
        public UseExpressionBodyForConversionOperatorsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_operators), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_operators), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   ImmutableArray.Create(SyntaxKind.ConversionOperatorDeclaration),
                   CSharpCodeStyleOptions.PreferExpressionBodiedOperators)
        {
        }

        protected override BlockSyntax GetBody(ConversionOperatorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(ConversionOperatorDeclarationSyntax declaration)
            => declaration.ExpressionBody;
    }
}