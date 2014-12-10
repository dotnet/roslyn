' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        ''' Spill field allocator controlls allocation and reusage of the set of fields 
        ''' used to spilling expressions; current implementation allows reuse of fields 
        ''' of the same type on high-level statement level
        ''' </summary>
        Private Class SpillFieldAllocator
            Private ReadOnly F As SyntheticBoundNodeFactory
            Private ReadOnly AllocatedFields As New KeyedStack(Of TypeSymbol, FieldSymbol)
            Private ReadOnly RealizedSpills As New HashSet(Of FieldSymbol)(ReferenceEqualityComparer.Instance)

            Friend Sub New(f As SyntheticBoundNodeFactory)
                Me.F = f
            End Sub

            Friend Function AllocateField(type As TypeSymbol) As FieldSymbol
                Dim field As FieldSymbol = Nothing
                If Not Me.AllocatedFields.TryPop(type, field) Then
                    field = F.StateMachineField(type,
                                              F.CurrentMethod,
                                              StringConstants.StateMachineStackSpillPrefix & Me.F.CompilationState.GenerateTempNumber(),
                                              Accessibility.Friend)
                End If
                Me.RealizedSpills.Add(field)
                Return field
            End Function

            Friend Sub FreeField(field As FieldSymbol)
                Debug.Assert(Me.RealizedSpills.Contains(field))
                Me.RealizedSpills.Remove(field)
                Me.AllocatedFields.Push(field.Type, field)
            End Sub

        End Class
    End Class

End Namespace
