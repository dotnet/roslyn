' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class ForLoopVerification

        ''' <summary>
        ''' A BoundForLoopStatement node has a list of control variables (from the attached next statement).
        ''' When binding the control variable of a for/for each loop that is nested in another for/for each loop, it must be
        ''' checked that the control variable has not been used by a containing for/for each loop. Because bound nodes do not
        ''' know their parents and we try to avoid passing around a stack of variables, we just walk the bound tree after the
        ''' initial binding to report this error.
        ''' In addition, it must be checked that the control variables of the next statement match the loop. Because the inner 
        ''' most loop contains the next with control variables from outer binders, checking this here is also convenient.
        '''
        ''' There are two diagnostics reported by this walker:
        ''' 1. BC30069: For loop control variable '{0}' already in use by an enclosing For loop.
        ''' 2. BC30070: Next control variable does not match For loop control variable '{0}'.
        ''' </summary>
        Public Shared Sub VerifyForLoops(block As BoundBlock, diagnostics As DiagnosticBag)
            Try
                Dim verifier As New ForLoopVerificationWalker(diagnostics)
                verifier.Visit(block)
            Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                ex.AddAnError(diagnostics)
            End Try
        End Sub

        Private NotInheritable Class ForLoopVerificationWalker
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private ReadOnly _diagnostics As DiagnosticBag
            Private ReadOnly _controlVariables As Stack(Of BoundExpression)

            Public Sub New(diagnostics As DiagnosticBag)
                _diagnostics = diagnostics
                _controlVariables = New Stack(Of BoundExpression)
            End Sub

            Public Overrides Function VisitForToStatement(node As BoundForToStatement) As BoundNode
                PreVisitForAndForEachStatement(node)
                MyBase.VisitForToStatement(node)
                PostVisitForAndForEachStatement(node)

                Return Nothing
            End Function

            Public Overrides Function VisitForEachStatement(node As BoundForEachStatement) As BoundNode
                PreVisitForAndForEachStatement(node)
                MyBase.VisitForEachStatement(node)
                PostVisitForAndForEachStatement(node)

                Return Nothing
            End Function

            ''' <summary>
            ''' Checks if the control variable was already used in an enclosing for loop
            ''' </summary>
            Private Sub PreVisitForAndForEachStatement(boundForStatement As BoundForStatement)
                ' check if the control variable is used previously in enclosing for loop blocks

                ' because the ExpressionSymbol is coming from the symbol declaration, the check cannot distinguish  
                ' a member access to a field from different or the same instance.
                ' This is consistent with VB6 and Dev10.
                Dim controlVariable = boundForStatement.ControlVariable
                Dim controlVariableSymbol = ForLoopVerification.ReferencedSymbol(controlVariable)
                Debug.Assert(controlVariableSymbol IsNot Nothing OrElse
                             controlVariable.Kind = BoundKind.BadExpression OrElse
                             controlVariable.HasErrors)

                ' Symbol may be Nothing for BadExpression.
                If controlVariableSymbol IsNot Nothing Then
                    For Each boundVariable In _controlVariables
                        If ForLoopVerification.ReferencedSymbol(boundVariable) = controlVariableSymbol Then
                            _diagnostics.Add(ERRID.ERR_ForIndexInUse1,
                                              controlVariable.Syntax.GetLocation(),
                                              CustomSymbolDisplayFormatter.ShortErrorName(controlVariableSymbol))
                            Exit For
                        End If
                    Next
                End If

                _controlVariables.Push(controlVariable)
            End Sub

            ''' <summary>
            ''' Checks if the control variables from the next statement match the control variable of the enclosing 
            ''' for loop.
            ''' Some loops may contain a next with multiple variables.
            ''' </summary>
            Private Sub PostVisitForAndForEachStatement(boundForStatement As BoundForStatement)
                ' do nothing if the array is nothing
                If Not boundForStatement.NextVariablesOpt.IsDefault Then

                    ' if it's empty we just have to adjust the index for one variable
                    If boundForStatement.NextVariablesOpt.IsEmpty Then
                        _controlVariables.Pop()
                    Else
                        For Each nextVariable In boundForStatement.NextVariablesOpt
                            ' m_controlVariables will not contain too much or too few elements because 
                            ' 1. parser will fill up with missing next statements
                            ' 2. binding will not bind spare control variables (see binding of next statement 
                            '    in BindForBlockParts)
                            Dim controlVariable = _controlVariables.Pop()

                            If Not controlVariable.HasErrors AndAlso
                                Not nextVariable.HasErrors AndAlso
                                ForLoopVerification.ReferencedSymbol(nextVariable) <> ForLoopVerification.ReferencedSymbol(controlVariable) Then
                                _diagnostics.Add(ERRID.ERR_NextForMismatch1,
                                                  nextVariable.Syntax.GetLocation(),
                                                  CustomSymbolDisplayFormatter.ShortErrorName(ForLoopVerification.ReferencedSymbol(controlVariable)))
                            End If
                        Next
                    End If
                End If
            End Sub

        End Class

        ''' <summary>
        ''' Gets the referenced symbol of the bound expression.
        ''' Used for matching variables between For and Next statements.
        ''' </summary>
        ''' <param name="expression">The bound expression.</param>
        Friend Shared Function ReferencedSymbol(expression As BoundExpression) As Symbol

            Select Case expression.Kind
                Case BoundKind.ArrayAccess
                    Return ReferencedSymbol(DirectCast(expression, BoundArrayAccess).Expression)
                Case BoundKind.PropertyAccess
                    Return DirectCast(expression, BoundPropertyAccess).PropertySymbol
                Case BoundKind.Call
                    Return DirectCast(expression, BoundCall).Method
                Case BoundKind.Local
                    Return DirectCast(expression, BoundLocal).LocalSymbol
                Case BoundKind.RangeVariable
                    Return DirectCast(expression, BoundRangeVariable).RangeVariable
                Case BoundKind.FieldAccess
                    Return DirectCast(expression, BoundFieldAccess).FieldSymbol
                Case BoundKind.Parameter
                    Return DirectCast(expression, BoundParameter).ParameterSymbol
                Case BoundKind.Parenthesized
                    Return ReferencedSymbol(DirectCast(expression, BoundParenthesized).Expression)
            End Select

            Return Nothing
        End Function
    End Class
End Namespace
