// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForMethodsDiagnosticAnalyzer : 
        AbstractUseExpressionBodyDiagnosticAnalyzer<MethodDeclarationSyntax>, IBuiltInAnalyzer
    {
        public UseExpressionBodyForMethodsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   ImmutableArray.Create(SyntaxKind.MethodDeclaration),
                   CSharpCodeStyleOptions.PreferExpressionBodiedMethods)
        {
        }

        protected override BlockSyntax GetBody(MethodDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(MethodDeclarationSyntax declaration)
            => declaration.ExpressionBody;
    }
}