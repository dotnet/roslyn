﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Represents a synthesized state machine helper field.
    ''' </summary>
    Friend NotInheritable Class StateMachineFieldSymbol
        Inherits SynthesizedFieldSymbol
        Implements ISynthesizedMethodBodyImplementationSymbol

        ' -1 if the field doesn't represent a long-lived local or an awaiter
        Friend ReadOnly SlotIndex As Integer

        Friend ReadOnly SlotDebugInfo As LocalSlotDebugInfo

        Public Sub New(stateMachineType As NamedTypeSymbol,
                      implicitlyDefinedBy As Symbol,
                      type As TypeSymbol,
                      name As String,
                      Optional accessibility As Accessibility = Accessibility.Private,
                      Optional isReadOnly As Boolean = False,
                      Optional isShared As Boolean = False,
                      Optional isSpecialNameAndRuntimeSpecial As Boolean = False)
            Me.New(stateMachineType,
                   implicitlyDefinedBy,
                   type,
                   name,
                   New LocalSlotDebugInfo(SynthesizedLocalKind.LoweringTemp, LocalDebugId.None),
                   slotIndex:=-1,
                   accessibility:=accessibility,
                   isReadOnly:=isReadOnly,
                   isShared:=isShared)
        End Sub

        Public Sub New(stateMachineType As NamedTypeSymbol,
                      implicitlyDefinedBy As Symbol,
                      type As TypeSymbol,
                      name As String,
                      synthesizedKind As SynthesizedLocalKind,
                      slotindex As Integer,
                      Optional accessibility As Accessibility = Accessibility.Private,
                      Optional isReadOnly As Boolean = False,
                      Optional isShared As Boolean = False,
                      Optional isSpecialNameAndRuntimeSpecial As Boolean = False)
            Me.New(stateMachineType,
                   implicitlyDefinedBy,
                   type,
                   name,
                   New LocalSlotDebugInfo(synthesizedKind, LocalDebugId.None),
                   slotIndex:=slotindex,
                   accessibility:=accessibility,
                   isReadOnly:=isReadOnly,
                   isShared:=isShared)
        End Sub

        Public Sub New(stateMachineType As NamedTypeSymbol,
                      implicitlyDefinedBy As Symbol,
                      type As TypeSymbol,
                      name As String,
                      slotDebugInfo As LocalSlotDebugInfo,
                      slotIndex As Integer,
                      Optional accessibility As Accessibility = Accessibility.Private,
                      Optional isReadOnly As Boolean = False,
                      Optional isShared As Boolean = False,
                      Optional isSpecialNameAndRuntimeSpecial As Boolean = False)
            MyBase.New(stateMachineType,
                       implicitlyDefinedBy,
                       type,
                       name,
                       accessibility,
                       isReadOnly,
                       isShared,
                       isSpecialNameAndRuntimeSpecial)

            Debug.Assert(type IsNot Nothing)

            Me.SlotIndex = slotIndex
            Me.SlotDebugInfo = slotDebugInfo
        End Sub

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Dim symbol As ISynthesizedMethodBodyImplementationSymbol = CType(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol)
                Return symbol.Method
            End Get
        End Property
    End Class

End Namespace
