' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging
    Partial Friend Class VisualBasicProximityExpressionsService
        Public Class Worker

            Private ReadOnly _syntaxTree As SyntaxTree
            Private ReadOnly _position As Integer

            Private _parentStatement As StatementSyntax
            Private ReadOnly _additionalTerms As New List(Of String)()
            Private ReadOnly _expressions As New List(Of ExpressionSyntax)()

            Public Sub New(syntaxTree As SyntaxTree, position As Integer)
                _syntaxTree = syntaxTree
                _position = position
            End Sub

            Friend Function [Do](cancellationToken As CancellationToken) As IList(Of String)
                Dim token = _syntaxTree.GetRoot(cancellationToken).FindToken(_position)

                _parentStatement = token.GetAncestor(Of StatementSyntax)()
                If _parentStatement Is Nothing Then
                    Return Nothing
                End If

                AddRelevantExpressions(_parentStatement, _expressions, includeDeclarations:=False)
                AddPrecedingRelevantExpressions()
                AddFollowingRelevantExpressions(cancellationToken)
                AddCurrentDeclaration()
                AddMethodParameters()
                AddCatchParameters()
                AddMeExpression()

                Dim terms = New List(Of String)()
                _expressions.Do(Sub(e) AddExpressionTerms(e, terms))
                terms.AddRange(_additionalTerms)

                Dim proximityExpressions = terms.Distinct().ToList()

                Return If(proximityExpressions.Count = 0, Nothing, proximityExpressions)
            End Function

            Private Sub AddPrecedingRelevantExpressions()
                Dim previousStatement = _parentStatement.GetPreviousStatement()

                If previousStatement IsNot Nothing Then
                    ' Note: FieldDeclaration statements are interesting as the current statement, 
                    ' but not as the preceding statement (as in dev12).
                    Select Case (previousStatement.Kind)
                        Case SyntaxKind.LocalDeclarationStatement,
                             SyntaxKind.CallStatement,
                             SyntaxKind.ExpressionStatement,
                             SyntaxKind.AddHandlerStatement,
                             SyntaxKind.RemoveHandlerStatement,
                             SyntaxKind.RaiseEventStatement,
                             SyntaxKind.YieldStatement,
                             SyntaxKind.ReturnStatement,
                             SyntaxKind.ReDimStatement,
                             SyntaxKind.EraseStatement,
                             SyntaxKind.MidAssignmentStatement,
                             SyntaxKind.SimpleAssignmentStatement,
                             SyntaxKind.AddAssignmentStatement,
                             SyntaxKind.SubtractAssignmentStatement,
                             SyntaxKind.MultiplyAssignmentStatement,
                             SyntaxKind.DivideAssignmentStatement,
                             SyntaxKind.IntegerDivideAssignmentStatement,
                             SyntaxKind.ExponentiateAssignmentStatement,
                             SyntaxKind.LeftShiftAssignmentStatement,
                             SyntaxKind.RightShiftAssignmentStatement,
                             SyntaxKind.ConcatenateAssignmentStatement

                            AddRelevantExpressions(previousStatement, _expressions, includeDeclarations:=True)
                    End Select
                End If
            End Sub

            Private Sub AddFollowingRelevantExpressions(cancellationToken As CancellationToken)
                Dim line = _syntaxTree.GetText(cancellationToken).Lines.IndexOf(_position)
                Dim nextStatement = _parentStatement.GetNextStatement()

                While nextStatement IsNot Nothing AndAlso _syntaxTree.GetText(cancellationToken).Lines.IndexOf(nextStatement.SpanStart) = line
                    AddRelevantExpressions(nextStatement, _expressions, includeDeclarations:=False)
                    nextStatement = nextStatement.GetNextStatement()
                End While
            End Sub

            Private Sub AddCurrentDeclaration()
                If TypeOf _parentStatement Is LocalDeclarationStatementSyntax OrElse
                   TypeOf _parentStatement Is FieldDeclarationSyntax Then
                    AddRelevantExpressions(_parentStatement, _expressions, includeDeclarations:=True)
                End If
            End Sub

            Private Sub AddMethodParameters()
                If TypeOf _parentStatement.Parent Is MethodBlockBaseSyntax Then
                    Dim methodBlock = DirectCast(_parentStatement.Parent, MethodBlockBaseSyntax)

                    If methodBlock.BlockStatement Is _parentStatement OrElse
                        methodBlock.Statements.FirstOrDefault() Is _parentStatement OrElse
                        methodBlock.EndBlockStatement Is _parentStatement Then

                        If methodBlock.BlockStatement.ParameterList IsNot Nothing Then
                            For Each p In methodBlock.BlockStatement.ParameterList.Parameters
                                _additionalTerms.Add(p.Identifier.Identifier.ValueText)
                            Next
                        End If
                    End If
                End If
            End Sub

            Private Sub AddCatchParameters()
                If TypeOf _parentStatement.Parent Is CatchBlockSyntax Then
                    Dim catchBlock = DirectCast(_parentStatement.Parent, CatchBlockSyntax)
                    If catchBlock.Statements.FirstOrDefault() Is _parentStatement AndAlso
                       catchBlock.CatchStatement.IdentifierName IsNot Nothing Then
                        _additionalTerms.Add(catchBlock.CatchStatement.IdentifierName.Identifier.ValueText)
                    End If
                End If
            End Sub

            Private Sub AddMeExpression()
                If Not InSharedContext() Then
                    _additionalTerms.Add("Me")
                End If
            End Sub

            Private Function InSharedContext() As Boolean
                Dim methodBlock = Me._parentStatement.GetAncestorOrThis(Of MethodBlockBaseSyntax)()
                If methodBlock IsNot Nothing AndAlso methodBlock.BlockStatement.Modifiers.Any(Function(t) t.Kind = SyntaxKind.SharedKeyword) Then
                    Return True ' // TODO: need to hit this with unit-tests
                End If

                Dim propertyBlock = Me._parentStatement.GetAncestorOrThis(Of PropertyBlockSyntax)()
                If propertyBlock IsNot Nothing AndAlso propertyBlock.PropertyStatement.Modifiers.Any(Function(t) t.Kind = SyntaxKind.SharedKeyword) Then
                    Return True ' // // TODO: need to hit this with unit-tests
                End If

                Dim typeBlock = Me._parentStatement.GetAncestorOrThis(Of TypeBlockSyntax)()
                If typeBlock IsNot Nothing AndAlso typeBlock.Kind = SyntaxKind.ModuleBlock Then
                    Return True
                End If

                Return False
            End Function

            Private Shared Sub AddExpressionTerms(e As ExpressionSyntax, terms As List(Of String))
                If e Is Nothing Then
                    Return
                End If

                terms.Add(e.ConvertToSingleLine().ToString())
            End Sub
        End Class
    End Class
End Namespace
