// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected override bool AreObjectInitializersSupported(SyntaxNodeAnalysisContext context)
        {
            // object initializers are only available in C# 3.0 and above.  Don't offer this refactoring
            // in projects targeting a lesser version.
            return ((CSharpParseOptions)context.Node.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp3;
        }

        protected override SyntaxKind GetObjectCreationSyntaxKind() => SyntaxKind.ObjectCreationExpression;

        protected override ISyntaxFactsService GetSyntaxFactsService() => CSharpSyntaxFactsService.Instance;
    }
}
