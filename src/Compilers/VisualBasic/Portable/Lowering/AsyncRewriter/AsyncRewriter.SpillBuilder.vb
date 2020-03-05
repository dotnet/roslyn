' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Private Structure SpillBuilder
            Private _locals As ArrayBuilder(Of LocalSymbol)
            Private _fields As ArrayBuilder(Of FieldSymbol)
            Private _statements As ArrayBuilder(Of BoundStatement)

            Friend Sub Free()
                If Me._locals IsNot Nothing Then
                    Me._locals.Free()
                    Me._locals = Nothing
                End If

                If Me._statements IsNot Nothing Then
                    Me._statements.Free()
                    Me._statements = Nothing
                End If

                If Me._fields IsNot Nothing Then
                    Me._fields.Free()
                    Me._fields = Nothing
                End If
            End Sub

            Friend Function BuildSequenceAndFree(F As SyntheticBoundNodeFactory, expression As BoundExpression) As BoundExpression
                If Not Me.IsEmpty Then
                    expression =
                        F.SpillSequence(If(Me._locals Is Nothing, ImmutableArray(Of LocalSymbol).Empty, Me._locals.ToImmutableAndFree()),
                                        If(Me._fields Is Nothing, ImmutableArray(Of FieldSymbol).Empty, Me._fields.ToImmutableAndFree()),
                                        If(Me._statements Is Nothing, ImmutableArray(Of BoundStatement).Empty, Me._statements.ToImmutableAndFree()),
                                        expression)
                    Me._locals = Nothing
                    Me._statements = Nothing
                    Me._fields = Nothing
                End If
                Return expression
            End Function

            Public ReadOnly Property IsEmpty As Boolean
                Get
                    Return Me._locals Is Nothing AndAlso Me._statements Is Nothing AndAlso Me._fields Is Nothing
                End Get
            End Property

            Friend Sub AddSpill(<[In]> ByRef spill As SpillBuilder)
                If Not spill.IsEmpty Then
                    AddRange(Me._locals, spill._locals)
                    AddRange(Me._fields, spill._fields)
                    AddRange(Me._statements, spill._statements)
                End If
            End Sub

            Friend Sub AddSpill(spill As BoundSpillSequence)
                AddRange(Me._locals, spill.Locals)
                AddRange(Me._fields, spill.SpillFields)
                AddRange(Me._statements, spill.Statements)
            End Sub

            Friend Sub AddFieldWithInitialization(field As FieldSymbol, init As BoundStatement)
                Debug.Assert(field IsNot Nothing)
                Debug.Assert(init IsNot Nothing)

                Add(Me._fields, field)
                Add(Me._statements, init)
            End Sub

            Friend Sub AddLocal(local As LocalSymbol)
                Add(Me._locals, local)
            End Sub

            Friend Sub AddLocals(locals As ImmutableArray(Of LocalSymbol))
                AddRange(Me._locals, locals)
            End Sub

            Friend Sub AddStatement(statement As BoundStatement)
                Add(Me._statements, statement)
            End Sub

            Friend Sub AssumeFieldsIfNeeded(<[In], Out> ByRef expression As BoundSpillSequence)
                Debug.Assert(expression IsNot Nothing)
                If Not expression.SpillFields.IsEmpty Then
                    AddRange(Me._fields, expression.SpillFields)
                    expression = expression.Update(expression.Locals,
                                                   ImmutableArray(Of FieldSymbol).Empty,
                                                   expression.Statements,
                                                   expression.ValueOpt,
                                                   expression.Type)
                End If
            End Sub

            Private Shared Sub EnsureArrayBuilder(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T))
                If array Is Nothing Then
                    array = ArrayBuilder(Of T).GetInstance()
                End If
            End Sub

            Private Shared Sub Add(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T), element As T)
                EnsureArrayBuilder(array)
                array.Add(element)
            End Sub

            Private Shared Sub AddRange(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T), other As ArrayBuilder(Of T))
                If other Is Nothing OrElse other.Count = 0 Then
                    Return
                End If

                EnsureArrayBuilder(array)
                array.AddRange(other)
            End Sub

            Private Shared Sub AddRange(Of T)(<[In], Out> ByRef array As ArrayBuilder(Of T), other As ImmutableArray(Of T))
                If other.IsEmpty Then
                    Return
                End If

                EnsureArrayBuilder(array)
                array.AddRange(other)
            End Sub

        End Structure

    End Class

End Namespace
