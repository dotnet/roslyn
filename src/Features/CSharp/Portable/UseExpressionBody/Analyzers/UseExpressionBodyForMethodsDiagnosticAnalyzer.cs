// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForMethodsDiagnosticAnalyzer : 
        AbstractUseExpressionBodyDiagnosticAnalyzer<MethodDeclarationSyntax>
    {
        public UseExpressionBodyForMethodsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                   ImmutableArray.Create(SyntaxKind.MethodDeclaration),
                   new UseExpressionBodyForMethodsHelper())
        {
        }
    }
}