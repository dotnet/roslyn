' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous
    Friend Class RemoveAsyncModifierHelpers
        Friend Shared Function RemoveAsyncKeyword(subOrFunctionStatement As MethodStatementSyntax) As MethodStatementSyntax
            Dim modifiers = subOrFunctionStatement.Modifiers
            Dim asyncTokenIndex = modifiers.IndexOf(SyntaxKind.AsyncKeyword)

            Dim newSubOrFunctionKeyword = subOrFunctionStatement.SubOrFunctionKeyword
            Dim newModifiers As SyntaxTokenList
            If asyncTokenIndex = 0 Then
                ' Have to move the trivia on the async token appropriately.
                Dim asyncLeadingTrivia = modifiers(0).LeadingTrivia

                If modifiers.Count > 1 Then
                    ' Move the trivia to the next modifier;
                    newModifiers = modifiers.Replace(
                        modifiers(1),
                        modifiers(1).WithPrependedLeadingTrivia(asyncLeadingTrivia))
                    newModifiers = newModifiers.RemoveAt(0)
                Else
                    ' move it to the 'sub' or 'function' keyword.
                    newModifiers = modifiers.RemoveAt(0)
                    newSubOrFunctionKeyword = newSubOrFunctionKeyword.WithPrependedLeadingTrivia(asyncLeadingTrivia)
                End If
            Else
                newModifiers = modifiers.RemoveAt(asyncTokenIndex)
            End If

            Dim newSubOrFunctionStatement = subOrFunctionStatement.WithModifiers(newModifiers).WithSubOrFunctionKeyword(newSubOrFunctionKeyword)
            Return newSubOrFunctionStatement
        End Function

        Friend Shared Function FixMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax) As SyntaxNode
            Dim header As LambdaHeaderSyntax = GetNewHeader(node)
            Return node.WithSubOrFunctionHeader(header).WithLeadingTrivia(node.GetLeadingTrivia())
        End Function

        Friend Shared Function FixSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax) As SingleLineLambdaExpressionSyntax
            Dim header As LambdaHeaderSyntax = GetNewHeader(node)
            Return node.WithSubOrFunctionHeader(header).WithLeadingTrivia(node.GetLeadingTrivia())
        End Function

        Private Shared Function GetNewHeader(node As LambdaExpressionSyntax) As LambdaHeaderSyntax
            Dim header = DirectCast(node.SubOrFunctionHeader, LambdaHeaderSyntax)
            Dim asyncKeywordIndex = header.Modifiers.IndexOf(SyntaxKind.AsyncKeyword)
            Dim newHeader = header.WithModifiers(header.Modifiers.RemoveAt(asyncKeywordIndex))
            Return newHeader
        End Function
    End Class
End Namespace
