// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForPropertiesDiagnosticAnalyzer :
        AbstractUseExpressionBodyDiagnosticAnalyzer<PropertyDeclarationSyntax>, IBuiltInAnalyzer
    {
        public UseExpressionBodyForPropertiesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_properties), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_properties), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   ImmutableArray.Create(SyntaxKind.PropertyDeclaration),
                   CSharpCodeStyleOptions.PreferExpressionBodiedProperties)
        {
        }

        protected override BlockSyntax GetBody(PropertyDeclarationSyntax declaration)
            => GetBodyFromSingleGetAccessor(declaration.AccessorList);

        protected override ArrowExpressionClauseSyntax GetExpressionBody(PropertyDeclarationSyntax declaration)
            => declaration.ExpressionBody;
    }
}