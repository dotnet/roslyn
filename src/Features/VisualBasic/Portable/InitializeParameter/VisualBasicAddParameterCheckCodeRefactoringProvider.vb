﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddParameterCheckCodeRefactoringProvider)), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.ChangeSignature)>
    Friend Class VisualBasicAddParameterCheckCodeRefactoringProvider
        Inherits AbstractAddParameterCheckCodeRefactoringProvider(Of
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function IsFunctionDeclaration(node As SyntaxNode) As Boolean
            Return InitializeParameterHelpers.IsFunctionDeclaration(node)
        End Function

        Protected Overrides Function GetTypeBlock(node As SyntaxNode) As SyntaxNode
            Return DirectCast(node, TypeStatementSyntax).Parent
        End Function

        Protected Overrides Function GetBody(functionDeclaration As SyntaxNode) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(functionDeclaration)
        End Function

        Protected Overrides Sub InsertStatement(editor As SyntaxEditor, functionDeclaration As SyntaxNode, method As IMethodSymbol, statementToAddAfterOpt As SyntaxNode, statement As StatementSyntax)
            InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, statementToAddAfterOpt, statement)
        End Sub

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination)
        End Function

        Protected Overrides Function CanOffer(body As SyntaxNode) As Boolean
            Return True
        End Function

        Protected Overrides Function PrefersThrowExpression(options As DocumentOptionSet) As Boolean
            ' No throw expression preference option is defined for VB because it doesn't support throw expressions.
            Return False
        End Function
    End Class
End Namespace
