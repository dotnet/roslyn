' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

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
                _F = f
                _nextHoistedFieldId = 0
            End Sub

            Friend Function AllocateField(type As TypeSymbol) As FieldSymbol
                Dim field As FieldSymbol = Nothing
                If Not _allocatedFields.TryPop(type, field) Then
                    _nextHoistedFieldId += 1

                    field = _F.StateMachineField(type,
                                              _F.CurrentMethod,
                                              GeneratedNames.ReusableHoistedLocalFieldName(_nextHoistedFieldId),
                                              Accessibility.Friend)
                End If
                _realizedSpills.Add(field)
                Return field
            End Function

            Friend Sub FreeField(field As FieldSymbol)
                Debug.Assert(_realizedSpills.Contains(field))
                _realizedSpills.Remove(field)
                _allocatedFields.Push(field.Type, field)
            End Sub

        End Class
    End Class

End Namespace
