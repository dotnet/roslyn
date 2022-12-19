' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A base class for symbols representing a synthesized property.
    ''' </summary>
    Friend MustInherit Class SynthesizedPropertyBase
        Inherits PropertySymbol

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

        Public NotOverridable Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return False
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
                Return False
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

        Public MustOverride Overrides ReadOnly Property IsImplicitlyDeclared As Boolean

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

        Public NotOverridable Overrides ReadOnly Property IsRequired As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

End Namespace
