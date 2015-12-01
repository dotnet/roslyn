' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A representation of a property symbol that is intended only to be used for comparison purposes
    ''' (esp in PropertySignatureComparer).
    ''' </summary>
    Friend Class SignatureOnlyPropertySymbol
        Inherits PropertySymbol

        Private ReadOnly _name As String
        Private ReadOnly _containingType As NamedTypeSymbol
        Private ReadOnly _isReadOnly As Boolean
        Private ReadOnly _isWriteOnly As Boolean
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnsByRef As Boolean
        Private ReadOnly _type As TypeSymbol
        Private ReadOnly _typeCustomModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _isOverrides As Boolean
        Private ReadOnly _isWithEvents As Boolean

        Public Sub New(name As String,
                       containingType As NamedTypeSymbol,
                       isReadOnly As Boolean,
                       isWriteOnly As Boolean,
                       parameters As ImmutableArray(Of ParameterSymbol),
                       returnsByRef As Boolean,
                       [type] As TypeSymbol,
                       typeCustomModifiers As ImmutableArray(Of CustomModifier),
                       Optional isOverrides As Boolean = False,
                       Optional isWithEvents As Boolean = False)
            _name = name
            _containingType = containingType
            _isReadOnly = isReadOnly
            _isWriteOnly = isWriteOnly
            _parameters = parameters
            _returnsByRef = returnsByRef
            _type = [type]
            _typeCustomModifiers = typeCustomModifiers
            _isOverrides = isOverrides
            _isWithEvents = isWithEvents
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
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

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _isReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsWriteOnly As Boolean
            Get
                Return _isWriteOnly
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _returnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _typeCustomModifiers
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

#Region "Not used by PropertySignatureComparer"
        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get

        End Property
        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _isOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                Return _isWithEvents
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of PropertySymbol)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property
#End Region
    End Class
End Namespace

