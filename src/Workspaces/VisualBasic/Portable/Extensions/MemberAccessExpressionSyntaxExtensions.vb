' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module MemberAccessExpressionSyntaxExtensions
        <Extension()>
        Public Function IsConstructorInitializer(memberAccess As MemberAccessExpressionSyntax) As Boolean
            Return memberAccess.IsThisConstructorInitializer() OrElse memberAccess.IsBaseConstructorInitializer()
        End Function

        <Extension()>
        Public Function IsThisConstructorInitializer(memberAccess As MemberAccessExpressionSyntax) As Boolean
            If memberAccess IsNot Nothing Then
                If IsFirstStatementInConstructor(memberAccess) Then
                    If memberAccess.Expression.IsKind(SyntaxKind.MeExpression) OrElse
                       memberAccess.Expression.IsKind(SyntaxKind.MyClassExpression) Then
                        If memberAccess.Name.IsKind(SyntaxKind.IdentifierName) Then
                            Return memberAccess.Name.Identifier.HasMatchingText(SyntaxKind.NewKeyword)
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsBaseConstructorInitializer(memberAccess As MemberAccessExpressionSyntax) As Boolean
            If memberAccess IsNot Nothing Then
                If IsFirstStatementInConstructor(memberAccess) Then
                    If memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression) Then
                        If memberAccess.Name.IsKind(SyntaxKind.IdentifierName) Then
                            Return memberAccess.Name.Identifier.HasMatchingText(SyntaxKind.NewKeyword)
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Private Function IsFirstStatementInConstructor(memberAccess As MemberAccessExpressionSyntax) As Boolean
            Dim isCall As Boolean
            Dim statement As SyntaxNode
            If TypeOf memberAccess.Parent Is InvocationExpressionSyntax Then
                statement = memberAccess.Parent.Parent
                isCall = statement IsNot Nothing AndAlso (statement.Kind = SyntaxKind.CallStatement OrElse statement.Kind = SyntaxKind.ExpressionStatement)
            Else
                statement = memberAccess.Parent
                isCall = statement.IsKind(SyntaxKind.CallStatement)
            End If

            If isCall Then
                Return statement.IsParentKind(SyntaxKind.ConstructorBlock) AndAlso
                    DirectCast(statement.Parent, ConstructorBlockSyntax).Statements.First() Is statement
            End If
            Return False

        End Function

        <Extension>
        Public Function GetExpressionOfMemberAccessExpression(memberAccessExpression As MemberAccessExpressionSyntax) As ExpressionSyntax
            If memberAccessExpression Is Nothing Then
                Return Nothing
            End If

            If memberAccessExpression.Expression IsNot Nothing Then
                Return memberAccessExpression.Expression
            End If

            ' Maybe we're part of a ConditionalAccessExpression
            Dim conditional = memberAccessExpression.GetCorrespondingConditionalAccessExpression()
            If conditional IsNot Nothing Then
                If conditional.Expression Is Nothing Then

                    ' No expression, maybe we're in a with block
                    Dim withBlock = conditional.GetAncestor(Of WithBlockSyntax)()
                    If withBlock IsNot Nothing Then
                        Return withBlock.WithStatement.Expression
                    End If
                End If

                Return conditional.Expression
            End If

            ' we have a member access expression with a null expression, this may be one of the
            ' following forms:
            '
            ' 1) new With { .a = 1, .b = .a     <-- .a refers to the anonymous type
            ' 2) With obj : .m                  <-- .m refers to the obj type
            ' 3) new T() With { .a = 1, .b = .a <-- 'a refers to the T type

            Dim current As SyntaxNode = memberAccessExpression

            While current IsNot Nothing
                If TypeOf current Is AnonymousObjectCreationExpressionSyntax Then
                    Return DirectCast(current, ExpressionSyntax)
                ElseIf TypeOf current Is WithBlockSyntax Then
                    Dim withBlock = DirectCast(current, WithBlockSyntax)
                    If memberAccessExpression IsNot withBlock.WithStatement.Expression Then
                        Return withBlock.WithStatement.Expression
                    End If
                ElseIf TypeOf current Is ObjectMemberInitializerSyntax AndAlso
                       TypeOf current.Parent Is ObjectCreationExpressionSyntax Then
                    Return DirectCast(current.Parent, ExpressionSyntax)
                End If

                current = current.Parent
            End While

            Return Nothing
        End Function
    End Module
End Namespace
