' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Private Structure SpillBuilder
            Private _locals As ArrayBuilder(Of LocalSymbol)
            Private _fields As ArrayBuilder(Of FieldSymbol)
            Private _statements As ArrayBuilder(Of BoundStatement)

            Friend Sub Free()
                If _locals IsNot Nothing Then
                    _locals.Free()
                    _locals = Nothing
                End If

                If _statements IsNot Nothing Then
                    _statements.Free()
                    _statements = Nothing
                End If

                If _fields IsNot Nothing Then
                    _fields.Free()
                    _fields = Nothing
                End If
            End Sub

            Friend Function BuildSequenceAndFree(F As SyntheticBoundNodeFactory, expression As BoundExpression) As BoundExpression
                If Not IsEmpty Then
                    expression =
                        F.SpillSequence(If(_locals Is Nothing, ImmutableArray(Of LocalSymbol).Empty, _locals.ToImmutableAndFree()),
                                        If(_fields Is Nothing, ImmutableArray(Of FieldSymbol).Empty, _fields.ToImmutableAndFree()),
                                        If(_statements Is Nothing, ImmutableArray(Of BoundStatement).Empty, _statements.ToImmutableAndFree()),
                                        expression)
                    _locals = Nothing
                    _statements = Nothing
                    _fields = Nothing
                End If
                Return expression
            End Function

            Public ReadOnly Property IsEmpty As Boolean
                Get
                    Return _locals Is Nothing AndAlso _statements Is Nothing AndAlso _fields Is Nothing
                End Get
            End Property

            Friend Sub AddSpill(<[In]> ByRef spill As SpillBuilder)
                If Not spill.IsEmpty Then
                    AddRange(_locals, spill._locals)
                    AddRange(_fields, spill._fields)
                    AddRange(_statements, spill._statements)
                End If
            End Sub

            Friend Sub AddSpill(spill As BoundSpillSequence)
                AddRange(_locals, spill.Locals)
                AddRange(_fields, spill.SpillFields)
                AddRange(_statements, spill.Statements)
            End Sub

            Friend Sub AddFieldWithInitialization(field As FieldSymbol, init As BoundStatement)
                Debug.Assert(field IsNot Nothing)
                Debug.Assert(init IsNot Nothing)

                Add(_fields, field)
                Add(_statements, init)
            End Sub

            Friend Sub AddLocal(local As LocalSymbol)
                Add(_locals, local)
            End Sub

            Friend Sub AddLocals(locals As ImmutableArray(Of LocalSymbol))
                AddRange(_locals, locals)
            End Sub

            Friend Sub AddStatement(statement As BoundStatement)
                Add(_statements, statement)
            End Sub

            Friend Sub AssumeFieldsIfNeeded(<[In], Out> ByRef expression As BoundSpillSequence)
                Debug.Assert(expression IsNot Nothing)
                If Not expression.SpillFields.IsEmpty Then
                    AddRange(_fields, expression.SpillFields)
                    expression = expression.Update(expression.Locals,
                                                   ImmutableArray(Of FieldSymbol).Empty,
                                                   expression.Statements,
                                                   expression.ValueOpt,
                                                   expression.Type)
                End If
            End Sub

            Private Sub EnsureArrayBuilder(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T))
                If array Is Nothing Then
                    array = ArrayBuilder(Of T).GetInstance()
                End If
            End Sub

            Private Sub Add(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T), element As T)
                EnsureArrayBuilder(array)
                array.Add(element)
            End Sub

            Private Sub AddRange(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T), other As ArrayBuilder(Of T))
                If other Is Nothing OrElse other.Count = 0 Then
                    Return
                End If

                EnsureArrayBuilder(array)
                array.AddRange(other)
            End Sub

            Private Sub AddRange(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T), other As ImmutableArray(Of T))
                If other.IsEmpty Then
                    Return
                End If

                EnsureArrayBuilder(array)
                array.AddRange(other)
            End Sub

        End Structure

    End Class

End Namespace
