' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module MemberAccessExpressionSyntaxExtensions
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
        Public Function GetExpressionOfMemberAccessExpression(
                memberAccessExpression As MemberAccessExpressionSyntax,
                Optional allowImplicitTarget As Boolean = False) As ExpressionSyntax
            If memberAccessExpression Is Nothing Then
                Return Nothing
            End If

            If memberAccessExpression.Expression IsNot Nothing Then
                Return memberAccessExpression.Expression
            End If

            ' we have a member access expression with a null expression, this may be one of the
            ' following forms:
            '
            ' 1) new With { .a = 1, .b = .a     <-- .a refers to the anonymous type
            ' 2) With obj : .m                  <-- .m refers to the obj type
            ' 3) new T() With { .a = 1, .b = .a <-- 'a refers to the T type

            If allowImplicitTarget Then
                Dim conditional = memberAccessExpression.GetRootConditionalAccessExpression()
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
            End If

            Return Nothing
        End Function
    End Module
End Namespace
