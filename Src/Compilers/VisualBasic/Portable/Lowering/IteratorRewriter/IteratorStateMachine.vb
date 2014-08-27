' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class IteratorStateMachine
        Inherits SynthesizedContainer
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly _constructor As SynthesizedSimpleConstructorSymbol
        Private ReadOnly _iteratorMethod As MethodSymbol

        Protected Friend Sub New(topLevelMethod As MethodSymbol,
                                 typeIndex As Integer,
                                 valueTypeSymbol As TypeSymbol,
                                 isEnumerable As Boolean)

            MyBase.New(topLevelMethod,
                       GeneratedNames.MakeStateMachineTypeName(typeIndex, topLevelMethod.Name),
                       topLevelMethod.ContainingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object),
                       GetIteratorInterfaces(valueTypeSymbol,
                                             isEnumerable,
                                             topLevelMethod.ContainingAssembly))

            Dim intType = DeclaringCompilation.GetSpecialType(SpecialType.System_Int32)

            Me._constructor = New SynthesizedSimpleConstructorSymbol(Me)
            Dim parameters = ImmutableArray.Create(Of ParameterSymbol)(
                New SynthesizedParameterSymbol(Me._constructor, intType, 0, False, GeneratedNames.MakeStateMachineStateFieldName()))

            Me._constructor.SetParameters(parameters)
            Me._iteratorMethod = topLevelMethod
        End Sub

        Private Shared Function GetIteratorInterfaces(elementType As TypeSymbol,
                                                      isEnumerable As Boolean,
                                                      containingAssembly As AssemblySymbol) As ImmutableArray(Of NamedTypeSymbol)

            Dim interfaces = ArrayBuilder(Of NamedTypeSymbol).GetInstance()

            If isEnumerable Then
                interfaces.Add(containingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Collections_Generic_IEnumerable_T).Construct(elementType))
                interfaces.Add(containingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Collections_IEnumerable))
            End If

            interfaces.Add(containingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Collections_Generic_IEnumerator_T).Construct(elementType))
            interfaces.Add(containingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_IDisposable))
            interfaces.Add(containingAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Collections_IEnumerator))

            Return interfaces.ToImmutableAndFree()
        End Function

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Class
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
            Get
                Return Me._constructor
            End Get
        End Property

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                ' This method contains user code from the iterator method
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return _iteratorMethod
            End Get
        End Property
    End Class

End Namespace