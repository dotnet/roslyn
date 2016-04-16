' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents synthetic WithEvents property that overrides
    ''' one in one of the base classes.
    ''' </summary>
    ''' <remarks>
    ''' When derived class contains "Handles" methods that refer to a 
    ''' WithEvents property in a base class, derived class needs to add a 
    ''' synthetic override for the base WithEvent property. 
    ''' We need the override so that we could inject code sequences that 
    ''' remove old handlers and add new handlers when something is assigned 
    ''' to the property.
    ''' <example>
    ''' Public Class Base
    '''     Protected Friend WithEvents w As Base = Me
    '''     Public Event e As System.Action
    ''' 
    '''     Sub H1() Handles w.e
    '''     End Sub
    ''' End Class
    ''' 
    ''' Public Class Derived
    '''     Inherits Base
    ''' 
    '''     Sub H2() Handles w.e
    '''     End Sub
    ''' End Class
    ''' </example>
    ''' </remarks>
    Friend NotInheritable Class SynthesizedOverridingWithEventsProperty
        Inherits PropertySymbol

        Private ReadOnly _baseProperty As PropertySymbol
        Private ReadOnly _containingType As SourceNamedTypeSymbol
        Private ReadOnly _getter As SynthesizedWithEventsAccessorSymbol
        Private ReadOnly _setter As SynthesizedWithEventsAccessorSymbol

        Friend Sub New(baseProperty As PropertySymbol, container As SourceNamedTypeSymbol)
            Me._baseProperty = baseProperty
            Me._containingType = container

            _getter = New SynthesizedWithEventsGetAccessorSymbol(
                container,
                Me)

            _setter = New SynthesizedWithEventsSetAccessorSymbol(
                container,
                Me,
                baseProperty.SetMethod.ReturnType,
                valueParameterName:=StringConstants.WithEventsValueParameterName)
        End Sub

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                'WithEvents properties shadow by name (similarly to fields).
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return _baseProperty.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _baseProperty.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return ImmutableArray(Of PropertySymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return _getter
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return _setter
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                ' override is itself overridable
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                ' that's the idea.
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                ' override is never shared
                Return False
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return _containingType.GetLexicalSortKey()
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _containingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Debug.Assert(_baseProperty.Parameters.IsEmpty)
                Return ImmutableArray(Of ParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Debug.Assert(_baseProperty.ParameterCount = 0)
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _baseProperty.Type
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _baseProperty.TypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                ' since it is IsImplicitlyDeclared
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _baseProperty.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
