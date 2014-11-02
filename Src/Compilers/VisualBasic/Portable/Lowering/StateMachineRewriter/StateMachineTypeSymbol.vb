' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class StateMachineTypeSymbol
        Inherits SynthesizedContainer
        Implements ISynthesizedMethodBodyImplementationSymbol

        Public ReadOnly KickoffMethod As MethodSymbol

        Public Sub New(slotAllocatorOpt As VariableSlotAllocator,
                       kickoffMethod As MethodSymbol,
                       baseType As NamedTypeSymbol,
                       originalInterfaces As ImmutableArray(Of NamedTypeSymbol))
            MyBase.New(kickoffMethod, MakeName(slotAllocatorOpt, kickoffMethod), baseType, originalInterfaces)

            Debug.Assert(kickoffMethod IsNot Nothing)

            ' If the async method is partial the kickoff method should be the method with body (implementation)
            Debug.Assert(kickoffMethod.IsDefinition)
            Debug.Assert(Not kickoffMethod.IsPartial OrElse kickoffMethod.PartialImplementationPart Is Nothing)

            Me.KickoffMethod = kickoffMethod
        End Sub

        Private Shared Function MakeName(slotAllocatorOpt As VariableSlotAllocator, kickoffMethod As MethodSymbol) As String
            Return If(slotAllocatorOpt?.PreviousStateMachineTypeName,
                      GeneratedNames.MakeStateMachineTypeName(SequenceNumber(kickoffMethod), kickoffMethod.Name))
        End Function

        Private Shared Function SequenceNumber(kickoffMethod As MethodSymbol) As Integer

            ' return a unique sequence number for the async implementation class that is independent of the compilation state.
            Dim count As Integer = 0
            For Each m In kickoffMethod.ContainingNamespaceOrType().GetMembers(kickoffMethod.Name)
                count += 1

                If kickoffMethod Is m Then
                    Return count
                End If
            Next

            ' It is possible we did not find any such members, e.g. for methods that result from the translation of
            ' async lambdas.  In that case the method has already been uniquely named, so there is no need to
            ' produce a unique sequence number for the corresponding class, which already includes the (unique) method name.
            Return count
        End Function

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                ' This method contains user code from the async method
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return KickoffMethod
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
