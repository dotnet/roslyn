' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class represents a symbol for the compiler generated property that owns implemented getter (like IEnumerable.Current),
    ''' The getter method being implemented is passed as a parameter and is used to build
    ''' implementation property around it with appropriate parameters, return value type, etc...
    ''' </summary>
    Friend NotInheritable Class SynthesizedStateMachineProperty
        Inherits SynthesizedPropertyBase
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly getter As SynthesizedStateMachineMethod
        Private ReadOnly propName As String

        Friend Sub New(containingType As NamedTypeSymbol,
                       name As String,
                       interfaceMethod As MethodSymbol,
                       syntax As VisualBasicSyntaxNode,
                       attributes As DebugAttributes,
                       declaredAccessibility As Accessibility,
                       enableDebugInfo As Boolean,
                       hasMethodBodyDependency As Boolean)

            Me.propName = name

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

            getter = New SynthesizedStateMachineMethod(containingType,
                                                         getterName,
                                                         interfaceMethod,
                                                         syntax,
                                                         attributes,
                                                         declaredAccessibility,
                                                         enableDebugInfo,
                                                         hasMethodBodyDependency,
                                                         associatedProperty:=Me)

        End Sub

        Private ReadOnly Property ImplementedProperty As PropertySymbol
            Get
                Return DirectCast(getter.ExplicitInterfaceImplementations(0).AssociatedSymbol, PropertySymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return ImmutableArray.Create(ImplementedProperty)
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return propName
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return getter.ContainingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return getter
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
                Return Me.getter.ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return Me.getter.Parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return Me.getter.ParameterCount
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me.getter.ReturnTypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me.getter.DeclaredAccessibility
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return Me.getter.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me.getter.IsShared
            End Get
        End Property

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return Me.getter.HasMethodBodyDependency
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return DirectCast(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol).Method
            End Get
        End Property
    End Class

End Namespace