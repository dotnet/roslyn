' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddParameterCheckCodeRefactoringProvider)), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.ChangeSignature)>
    Friend Class VisualBasicAddParameterCheckCodeRefactoringProvider
        Inherits AbstractAddParameterCheckCodeRefactoringProvider(Of
            ParameterSyntax,
            MethodBlockBaseSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax)

        Protected Overrides Function GetTypeBlock(node As SyntaxNode) As SyntaxNode
            Return DirectCast(node, TypeStatementSyntax).Parent
        End Function

        Protected Overrides Function GetBody(containingMember As MethodBlockBaseSyntax) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(containingMember)
        End Function

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination)
        End Function

        Protected Overrides Sub InsertStatement(editor As SyntaxEditor, methodDeclaration As MethodBlockBaseSyntax, statementToAddAfterOpt As SyntaxNode, statement As StatementSyntax)
            InitializeParameterHelpers.InsertStatement(editor, methodDeclaration, statementToAddAfterOpt, statement)
        End Sub

        Protected Overrides Function CanOffer(blockStatement As SyntaxNode) As Boolean
            Return True
        End Function
    End Class
End Namespace
