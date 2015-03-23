' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Class ExpressionSyntaxGeneratorVisitor
        Inherits SymbolVisitor(Of ExpressionSyntax)

        ' Public Shared ReadOnly Instance As ExpressionSyntaxGeneratorVisitor = New ExpressionSyntaxGeneratorVisitor()

        Private ReadOnly _addGlobal As Boolean

        Public Sub New(addGlobal As Boolean)
            Me._addGlobal = addGlobal
        End Sub

        Public Overrides Function DefaultVisit(symbol As ISymbol) As ExpressionSyntax
            Return symbol.Accept(New TypeSyntaxGeneratorVisitor(_addGlobal))
        End Function

        Private Function AddInformationTo(Of TExpressionSyntax As ExpressionSyntax)(expression As TExpressionSyntax, symbol As ISymbol) As TExpressionSyntax
            expression = expression.WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
            expression = expression.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol))
            Return expression
        End Function

        Public Overrides Function VisitNamedType(symbol As INamedTypeSymbol) As ExpressionSyntax
            Dim typeSyntax = New TypeSyntaxGeneratorVisitor(_addGlobal).CreateSimpleTypeSyntax(symbol)
            If Not (TypeOf typeSyntax Is SimpleNameSyntax) Then
                Return typeSyntax
            End If

            Dim simpleNameSyntax = DirectCast(typeSyntax, SimpleNameSyntax)
            If symbol.ContainingType IsNot Nothing Then
                If symbol.ContainingType.TypeKind = TypeKind.Submission Then
                    Return simpleNameSyntax
                Else
                    Dim container = symbol.ContainingType.Accept(Me)
                    Return CreateMemberAccessExpression(symbol, container, simpleNameSyntax)
                End If
            ElseIf symbol.ContainingNamespace IsNot Nothing Then
                If symbol.ContainingNamespace.IsGlobalNamespace Then
                    If symbol.TypeKind <> TypeKind.[Error] Then
                        Return CreateMemberAccessExpression(symbol, SyntaxFactory.GlobalName(), simpleNameSyntax)
                    End If
                Else
                    Dim container = symbol.ContainingNamespace.Accept(Me)
                    Return CreateMemberAccessExpression(symbol, container, simpleNameSyntax)
                End If
            End If

            Return simpleNameSyntax
        End Function

        Public Overrides Function VisitNamespace(symbol As INamespaceSymbol) As ExpressionSyntax
            Dim result = AddInformationTo(symbol.Name.ToIdentifierName, symbol)
            If symbol.ContainingNamespace Is Nothing Then
                Return result
            End If

            If symbol.ContainingNamespace.IsGlobalNamespace Then
                Return CreateMemberAccessExpression(symbol, SyntaxFactory.GlobalName(), result)
            Else
                Dim container = symbol.ContainingNamespace.Accept(Me)
                Return CreateMemberAccessExpression(symbol, container, result)
            End If
        End Function

        Private Function CreateMemberAccessExpression(symbol As ISymbol, container As ExpressionSyntax, simpleName As SimpleNameSyntax) As ExpressionSyntax
            Return AddInformationTo(SyntaxFactory.SimpleMemberAccessExpression(container, SyntaxFactory.Token(SyntaxKind.DotToken), simpleName), symbol)
        End Function
    End Class
End Namespace
