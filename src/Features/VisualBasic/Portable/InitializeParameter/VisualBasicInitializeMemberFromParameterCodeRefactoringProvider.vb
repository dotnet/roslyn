﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicInitializeMemberFromParameterCodeRefactoringProvider)), [Shared]>
    <ExtensionOrder(Before:=NameOf(VisualBasicAddParameterCheckCodeRefactoringProvider))>
    Friend Class VisualBasicInitializeMemberFromParameterCodeRefactoringProvider
        Inherits AbstractInitializeMemberFromParameterCodeRefactoringProvider(Of
            ParameterSyntax,
            ParameterListSyntax,
            StatementSyntax,
            ExpressionSyntax)

        Protected Overrides Function GetFunctionDeclaration(parameterList As ParameterListSyntax) As SyntaxNode
            Return InitializeParameterHelpers.GetFunctionDeclaration(parameterList)
        End Function

        Protected Overrides Function GetTypeBlock(node As SyntaxNode) As SyntaxNode
            Return DirectCast(node, TypeStatementSyntax).Parent
        End Function

        Protected Overrides Function TryGetLastStatement(blockStatement As IBlockOperation) As SyntaxNode
            Return DirectCast(blockStatement.Syntax, MethodBlockBaseSyntax).Statements.LastOrDefault()
        End Function

        Protected Overrides Function GetBody(functionDeclaration As SyntaxNode) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(DirectCast(functionDeclaration, MethodBlockBaseSyntax))
        End Function

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination)
        End Function

        Protected Overrides Sub InsertStatement(editor As SyntaxEditor, functionDeclaration As SyntaxNode, statementToAddAfterOpt As SyntaxNode, statement As StatementSyntax)
            InitializeParameterHelpers.InsertStatement(editor, DirectCast(functionDeclaration, MethodBlockBaseSyntax), statementToAddAfterOpt, statement)
        End Sub
    End Class
End Namespace
