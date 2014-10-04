' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class AsyncStateMachine
        Inherits StateMachineTypeSymbol

        Private ReadOnly _typeKind As TypeKind
        Private ReadOnly _constructor As SynthesizedSimpleConstructorSymbol

        Protected Friend Sub New(kickoffMethod As MethodSymbol,
                                 typeIndex As Integer,
                                 typeKind As TypeKind,
                                 valueTypeSymbol As NamedTypeSymbol,
                                 iAsyncStateMachineInterface As NamedTypeSymbol)

            MyBase.New(kickoffMethod,
                       GeneratedNames.MakeStateMachineTypeName(typeIndex, kickoffMethod.Name),
                       If(typeKind = TypeKind.Class, valueTypeSymbol.BaseTypeNoUseSiteDiagnostics, valueTypeSymbol),
                       ImmutableArray.Create(iAsyncStateMachineInterface))

            Me._constructor = New SynthesizedSimpleConstructorSymbol(Me)
            Me._constructor.SetParameters(ImmutableArray(Of ParameterSymbol).Empty)
            Me._typeKind = typeKind
        End Sub

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