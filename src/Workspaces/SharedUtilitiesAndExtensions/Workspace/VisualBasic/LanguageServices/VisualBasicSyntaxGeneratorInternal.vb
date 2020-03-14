' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGeneratorInternal), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxGeneratorInternal
        Inherits SyntaxGeneratorInternal

        Public Shared ReadOnly Instance As SyntaxGeneratorInternal = New VisualBasicSyntaxGeneratorInternal()

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Friend Overloads Overrides Function LocalDeclarationStatement(type As SyntaxNode, identifier As SyntaxToken, Optional initializer As SyntaxNode = Nothing, Optional isConst As Boolean = False) As SyntaxNode
            Return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(If(isConst, SyntaxKind.ConstKeyword, SyntaxKind.DimKeyword))),
                SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, SyntaxFactory.ModifiedIdentifier(identifier), initializer)))
        End Function

        Friend Overrides Function WithInitializer(variableDeclarator As SyntaxNode, initializer As SyntaxNode) As SyntaxNode
            Return DirectCast(variableDeclarator, VariableDeclaratorSyntax).WithInitializer(DirectCast(initializer, EqualsValueSyntax))
        End Function

        Friend Overrides Function EqualsValueClause(operatorToken As SyntaxToken, value As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.EqualsValue(operatorToken, DirectCast(value, ExpressionSyntax))
        End Function

        Friend Shared Function VariableDeclarator(type As SyntaxNode, name As ModifiedIdentifierSyntax, Optional expression As SyntaxNode = Nothing) As VariableDeclaratorSyntax
            Return SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(name),
                If(type Is Nothing, Nothing, SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))),
                If(expression Is Nothing,
                   Nothing,
                   SyntaxFactory.EqualsValue(DirectCast(expression, ExpressionSyntax))))
        End Function

        Friend Overrides Function Identifier(text As String) As SyntaxToken
            Return SyntaxFactory.Identifier(text)
        End Function
    End Class
End Namespace
