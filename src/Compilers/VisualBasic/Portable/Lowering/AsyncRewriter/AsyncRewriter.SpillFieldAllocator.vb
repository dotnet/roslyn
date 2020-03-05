' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        ''' <summary>
        ''' Spill field allocator controls allocation and reuse of the set of fields 
        ''' used to spilling expressions; current implementation allows reuse of fields 
        ''' of the same type on high-level statement level
        ''' </summary>
        Private Class SpillFieldAllocator
            Private ReadOnly _F As SyntheticBoundNodeFactory
            Private ReadOnly _allocatedFields As New KeyedStack(Of TypeSymbol, FieldSymbol)
            Private ReadOnly _realizedSpills As New HashSet(Of FieldSymbol)(ReferenceEqualityComparer.Instance)

            Private _nextHoistedFieldId As Integer

            Friend Sub New(f As SyntheticBoundNodeFactory)
                Me._F = f
                Me._nextHoistedFieldId = 0
            End Sub

            Friend Function AllocateField(type As TypeSymbol) As FieldSymbol
                Dim field As FieldSymbol = Nothing
                If Not Me._allocatedFields.TryPop(type, field) Then
                    _nextHoistedFieldId += 1

                    field = _F.StateMachineField(type,
                                              _F.CurrentMethod,
                                              GeneratedNames.ReusableHoistedLocalFieldName(_nextHoistedFieldId),
                                              Accessibility.Friend)
                End If
                Me._realizedSpills.Add(field)
                Return field
            End Function

            Friend Sub FreeField(field As FieldSymbol)
                Debug.Assert(Me._realizedSpills.Contains(field))
                Me._realizedSpills.Remove(field)
                Me._allocatedFields.Push(field.Type, field)
            End Sub

        End Class
    End Class

End Namespace
