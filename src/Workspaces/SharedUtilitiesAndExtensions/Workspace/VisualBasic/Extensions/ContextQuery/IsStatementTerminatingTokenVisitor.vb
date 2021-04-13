' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

    ''' <summary>
    ''' A visitor that determines if the targetToken passed in the constructor can be considered
    ''' the end of the visited statement. Tokens in the token stream of the file after
    ''' targetToken are ignored. This means that in some cases, say "Throw" vs. "Throw x" there
    ''' is more than one keyword that could terminate the statement.
    ''' </summary>
    Friend Class IsStatementTerminatingTokenVisitor
        Inherits VisualBasicSyntaxVisitor(Of Boolean)

        Private ReadOnly _targetToken As SyntaxToken

        Public Sub New(targetToken As SyntaxToken)
            _targetToken = targetToken
        End Sub

        Public Overrides Function DefaultVisit(node As SyntaxNode) As Boolean
            ' By default, it doesn't terminate
            Return False
        End Function

        Public Overrides Function VisitAddRemoveHandlerStatement(node As AddRemoveHandlerStatementSyntax) As Boolean
            Return TargetTokenMatches(GetExpressionTerminatingToken(node.DelegateExpression))
        End Function

        Public Overrides Function VisitAssignmentStatement(node As AssignmentStatementSyntax) As Boolean
            Return TargetTokenMatches(GetExpressionTerminatingToken(node.Right))
        End Function

        Public Overrides Function VisitCallStatement(node As CallStatementSyntax) As Boolean
            Return TargetTokenMatches(GetExpressionTerminatingToken(node.Invocation))
        End Function

        Public Overrides Function VisitExpressionStatement(node As ExpressionStatementSyntax) As Boolean
            Return TargetTokenMatches(GetExpressionTerminatingToken(node.Expression))
        End Function

        Public Overrides Function VisitContinueStatement(node As ContinueStatementSyntax) As Boolean
            Return TargetTokenMatches(node.BlockKeyword)
        End Function

        Public Overrides Function VisitEraseStatement(node As EraseStatementSyntax) As Boolean
            Return TargetTokenMatches(GetExpressionTerminatingToken(node.Expressions.Last()))
        End Function

        Public Overrides Function VisitErrorStatement(node As ErrorStatementSyntax) As Boolean
            Return TargetTokenMatches(GetExpressionTerminatingToken(node.ErrorNumber))
        End Function

        Public Overrides Function VisitExitStatement(node As ExitStatementSyntax) As Boolean
            Return TargetTokenMatches(node.BlockKeyword)
        End Function

        Public Overrides Function VisitGoToStatement(node As GoToStatementSyntax) As Boolean
            Return TargetTokenMatches(node.Label.LabelToken)
        End Function

        Public Overrides Function VisitLocalDeclarationStatement(node As LocalDeclarationStatementSyntax) As Boolean
            Dim lastDeclarator = node.Declarators.Last()

            If lastDeclarator.Initializer IsNot Nothing Then
                Return TargetTokenMatches(GetExpressionTerminatingToken(lastDeclarator.Initializer.Value))
            ElseIf lastDeclarator.AsClause IsNot Nothing Then
                Return TargetTokenMatches(GetExpressionTerminatingToken(lastDeclarator.AsClause.Type))
            Else
                Return TargetTokenMatches(lastDeclarator.Names.Last().Identifier)
            End If
        End Function

        Public Overrides Function VisitRaiseEventStatement(node As RaiseEventStatementSyntax) As Boolean
            Dim argumentList = node.ArgumentList
            If argumentList IsNot Nothing Then
                Return TargetTokenMatches(argumentList.CloseParenToken)
            Else
                Return TargetTokenMatches(node.Name.Identifier)
            End If
        End Function

        Public Overrides Function VisitReDimStatement(node As ReDimStatementSyntax) As Boolean
            Dim lastClause = node.Clauses.Last()
            If lastClause.ArrayBounds IsNot Nothing Then
                Return TargetTokenMatches(lastClause.ArrayBounds.CloseParenToken)
            Else
                Return TargetTokenMatches(GetExpressionTerminatingToken(lastClause.Expression))
            End If
        End Function

        Public Overrides Function VisitResumeStatement(node As ResumeStatementSyntax) As Boolean
            If node.Label IsNot Nothing AndAlso TargetTokenMatches(node.Label.LabelToken) Then
                Return True
            End If

            Return TargetTokenMatches(node.ResumeKeyword)
        End Function

        Public Overrides Function VisitReturnStatement(node As Microsoft.CodeAnalysis.VisualBasic.Syntax.ReturnStatementSyntax) As Boolean
            ' Do we need a return value?
            Dim methodBlock = node.ReturnKeyword.GetAncestor(Of MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing AndAlso methodBlock.IsKind(SyntaxKind.FunctionBlock, SyntaxKind.GetAccessorBlock) Then
                If node.Expression IsNot Nothing Then
                    If TargetTokenMatches(GetExpressionTerminatingToken(node.Expression)) Then
                        ' We are terminating the return value properly, so we're good
                        Return True
                    End If
                End If
            End If

            Return TargetTokenMatches(node.ReturnKeyword)
        End Function

        Public Overrides Function VisitStopOrEndStatement(node As StopOrEndStatementSyntax) As Boolean
            Return TargetTokenMatches(node.StopOrEndKeyword)
        End Function

        Public Overrides Function VisitThrowStatement(node As ThrowStatementSyntax) As Boolean
            If node.ThrowKeyword.HasAncestor(Of TryBlockSyntax)() Then
                If node.Expression IsNot Nothing Then
                    If TargetTokenMatches(GetExpressionTerminatingToken(node.Expression)) Then
                        Return True
                    End If
                End If
            End If

            Return TargetTokenMatches(node.ThrowKeyword)
        End Function

        Private Function TargetTokenMatches(token As SyntaxToken) As Boolean
            Return token.Kind <> SyntaxKind.None AndAlso Not token.IsMissing AndAlso _targetToken = token
        End Function
    End Class
End Namespace
