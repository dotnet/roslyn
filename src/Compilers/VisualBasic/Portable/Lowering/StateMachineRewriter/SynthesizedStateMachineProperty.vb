' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class represents a symbol for the compiler generated property that owns implemented getter (like IEnumerable.Current),
    ''' The getter method being implemented is passed as a parameter and is used to build
    ''' implementation property around it with appropriate parameters, return value type, etc...
    ''' </summary>
    Friend NotInheritable Class SynthesizedStateMachineProperty
        Inherits SynthesizedPropertyBase
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly _getter As SynthesizedStateMachineMethod
        Private ReadOnly _name As String

        Friend Sub New(stateMachineType As StateMachineTypeSymbol,
                       name As String,
                       interfacePropertyGetter As MethodSymbol,
                       syntax As SyntaxNode,
                       declaredAccessibility As Accessibility)

            Me._name = name

            ' If property is named Current, then getter is named get_Current
            ' the getter is named IEnumerator.get_Current
            Dim getterName As String
            If name.Length = 7 Then          ' "Current".Length 
                Debug.Assert(name = "Current")
                getterName = "get_Current"
            Else
                Debug.Assert(name = "IEnumerator.Current")
                getterName = "IEnumerator.get_Current"
            End If

            _getter = New SynthesizedStateMachineDebuggerNonUserCodeMethod(stateMachineType,
                                                                           getterName,
                                                                           interfacePropertyGetter,
                                                                           syntax,
                                                                           declaredAccessibility,
                                                                           hasMethodBodyDependency:=False,
                                                                           associatedProperty:=Me)

        End Sub

        Private ReadOnly Property ImplementedProperty As PropertySymbol
            Get
                Return DirectCast(_getter.ExplicitInterfaceImplementations(0).AssociatedSymbol, PropertySymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return ImmutableArray.Create(ImplementedProperty)
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _getter.ContainingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return _getter
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._getter.ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return Me._getter.Parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return Me._getter.ParameterCount
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._getter.ReturnTypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me._getter.DeclaredAccessibility
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return Me._getter.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me._getter.IsShared
            End Get
        End Property

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return Me._getter.HasMethodBodyDependency
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbolInternal Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return DirectCast(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol).Method
            End Get
        End Property
    End Class

End Namespace
