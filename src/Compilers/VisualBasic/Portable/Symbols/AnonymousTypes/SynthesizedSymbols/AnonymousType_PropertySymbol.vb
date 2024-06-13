' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousTypePropertySymbol
            Inherits PropertySymbol

            Private ReadOnly _containingType As AnonymousTypeTemplateSymbol
            Private ReadOnly _type As TypeSymbol
            Private ReadOnly _name As String

            Private ReadOnly _getMethod As MethodSymbol
            Private ReadOnly _setMethod As MethodSymbol
            Private ReadOnly _backingField As FieldSymbol

            ''' <summary> Index of the property in the containing anonymous type </summary>
            Friend ReadOnly PropertyIndex As Integer

            Public Sub New(container As AnonymousTypeTemplateSymbol, field As AnonymousTypeField, index As Integer, typeSymbol As TypeSymbol)

                _containingType = container

                _type = typeSymbol
                _name = field.Name
                PropertyIndex = index

                _getMethod = New AnonymousTypePropertyGetAccessorSymbol(Me)
                If Not field.IsKey Then
                    _setMethod = New AnonymousTypePropertySetAccessorSymbol(Me, container.Manager.System_Void)
                End If
                _backingField = New AnonymousTypePropertyBackingFieldSymbol(Me)
            End Sub

            Friend ReadOnly Property AnonymousType As AnonymousTypeTemplateSymbol
                Get
                    Return _containingType
                End Get
            End Property

            Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
                Get
                    Return _backingField
                End Get
            End Property

            Public Overrides ReadOnly Property IsDefault As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return ImmutableArray(Of ParameterSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return ImmutableArray(Of CustomModifier).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return ImmutableArray(Of CustomModifier).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property SetMethod As MethodSymbol
                Get
                    Return _setMethod
                End Get
            End Property

            Public Overrides ReadOnly Property GetMethod As MethodSymbol
                Get
                    Return _getMethod
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnsByRef As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return _type
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return Me._name
                End Get
            End Property

            Public Overrides ReadOnly Property MetadataName As String
                Get
                    Return Me.AnonymousType.GetAdjustedName(Me.PropertyIndex)
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Public
                End Get
            End Property

            Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
                Get
                    Return Microsoft.Cci.CallingConvention.HasThis
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

            Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
                Get
                    Return ImmutableArray(Of PropertySymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property IsMustOverride As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsNotOverridable As Boolean
                Get
                    Return False ' property is not virtual by default
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverloads As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverridable As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverrides As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
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

            Public Overrides ReadOnly Property IsRequired As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class

    End Class

End Namespace
