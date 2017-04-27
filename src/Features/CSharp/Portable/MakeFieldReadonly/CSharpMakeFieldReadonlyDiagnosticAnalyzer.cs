// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MakeFieldReadonly;

namespace Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpMakeFieldReadonlyDiagnosticAnalyzer :
        AbstractMakeFieldReadonlyDiagnosticAnalyzer<ConstructorDeclarationSyntax>
    {
        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

        internal override bool CanBeReadonly(SemanticModel model, SyntaxNode node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignmentNode && assignmentNode.Left == node)
            {
                return false;
            }
            
            if (node.Parent is ArgumentSyntax argumentNode && argumentNode.RefOrOutKeyword.Kind() != SyntaxKind.None)
            {
                return false;
            }

            if (node.Parent is PostfixUnaryExpressionSyntax postFixExpressionNode)
            {
                return false;
            }

            if (node.Parent is PrefixUnaryExpressionSyntax preFixExpressionNode)
            {
                return false;
            }

            return true;
        }
    }
}
