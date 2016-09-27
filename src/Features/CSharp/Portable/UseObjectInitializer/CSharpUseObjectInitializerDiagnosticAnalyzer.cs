// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseObjectInitializerDiagnosticAnalyzer :
        AbstractUseObjectInitializerDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        protected override bool FadeOutOperatorToken => true;

        protected override SyntaxKind GetObjectCreationSyntaxKind() => SyntaxKind.ObjectCreationExpression;

        protected override ISyntaxFactsService GetSyntaxFactsService() => CSharpSyntaxFactsService.Instance;
    }
}