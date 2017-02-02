// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MakeMethodStatic;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodStatic
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpMakeMethodStaticDiagnosticAnalyzer : AbstractMakeMethodStaticDiagnosticAnalyzer<SyntaxKind, MethodDeclarationSyntax>
    {
        public CSharpMakeMethodStaticDiagnosticAnalyzer()
            : base(new LocalizableResourceString(nameof(CSharpFeaturesResources.Make_method_static), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                   new LocalizableResourceString(nameof(CSharpFeaturesResources.Method_can_be_made_static), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
        {
        }

        protected override SyntaxKind GetMethodDeclarationSyntaxKind()
            => SyntaxKind.MethodDeclaration;

        protected override SyntaxNode GetBody(MethodDeclarationSyntax declaration)
            => (SyntaxNode)declaration.Body ?? declaration.ExpressionBody?.Expression;
    }
}
