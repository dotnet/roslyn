' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class AsyncStateMachine
        Inherits StateMachineTypeSymbol

        Private ReadOnly _typeKind As TypeKind
        Private ReadOnly _constructor As SynthesizedSimpleConstructorSymbol

        Protected Friend Sub New(slotAllocatorOpt As VariableSlotAllocator, compilationState As TypeCompilationState, asyncMethod As MethodSymbol, asyncMethodOrdinal As Integer, typeKind As TypeKind)
            MyBase.New(slotAllocatorOpt,
                       compilationState,
                       asyncMethod,
                       asyncMethodOrdinal,
                       asyncMethod.ContainingAssembly.GetSpecialType(If(typeKind = TypeKind.Struct, SpecialType.System_ValueType, SpecialType.System_Object)),
                       ImmutableArray.Create(asyncMethod.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine)))

            _constructor = New SynthesizedSimpleConstructorSymbol(Me)
            _constructor.SetParameters(ImmutableArray(Of ParameterSymbol).Empty)
            _typeKind = typeKind
        End Sub

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return _typeKind
            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
            Get
                Return _constructor
            End Get
        End Property
    End Class
End Namespace
