' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class AsyncStateMachine
        Inherits StateMachineTypeSymbol

        Private ReadOnly _typeKind As TypeKind
        Private ReadOnly _constructor As SynthesizedSimpleConstructorSymbol

        Protected Friend Sub New(asyncMethod As MethodSymbol, typeKind As TypeKind)
            MyBase.New(asyncMethod,
                       GeneratedNames.MakeStateMachineTypeName(SequenceNumber(asyncMethod), asyncMethod.Name),
                       asyncMethod.ContainingAssembly.GetSpecialType(If(typeKind = TypeKind.Struct, SpecialType.System_ValueType, SpecialType.System_Object)),
                       ImmutableArray.Create(asyncMethod.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine)))

            Me._constructor = New SynthesizedSimpleConstructorSymbol(Me)
            Me._constructor.SetParameters(ImmutableArray(Of ParameterSymbol).Empty)
            Me._typeKind = typeKind
        End Sub

        Private Shared Function SequenceNumber(method As MethodSymbol) As Integer

            ' return a unique sequence number for the async implementation class that is independent of the compilation state.
            Dim count As Integer = 0
            For Each m In method.ContainingNamespaceOrType().GetMembers(method.Name)
                count += 1

                If method Is m Then
                    Return count
                End If
            Next

            ' It is possible we did not find any such members, e.g. for methods that result from the translation of
            ' async lambdas.  In that case the method has already been uniquely named, so there is no need to
            ' produce a unique sequence number for the corresponding class, which already includes the (unique) method name.
            Return count
        End Function

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return Me._typeKind
            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
            Get
                Return Me._constructor
            End Get
        End Property
    End Class
End Namespace