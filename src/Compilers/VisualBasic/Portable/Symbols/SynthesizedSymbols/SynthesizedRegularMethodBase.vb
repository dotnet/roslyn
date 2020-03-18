' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents a base class for compiler generated methods
    ''' </summary>
    Friend MustInherit Class SynthesizedRegularMethodBase
        Inherits SynthesizedMethodBase

        Protected ReadOnly m_name As String
        Protected ReadOnly m_isShared As Boolean
        Protected ReadOnly m_SyntaxNode As VisualBasicSyntaxNode

        Protected Sub New(
            syntaxNode As VisualBasicSyntaxNode,
            container As NamedTypeSymbol,
            name As String,
            Optional isShared As Boolean = False
        )
            MyBase.New(container)

            m_SyntaxNode = syntaxNode
            m_isShared = isShared
            m_name = name
        End Sub

        ''' <summary>
        ''' Gets the symbol name. Returns the empty string if unnamed.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is abstract or not.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is abstract; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is not overridable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is not overridable; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overloads.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overloads; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overridable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overridable; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overrides.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overrides; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is shared.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is shared; otherwise, <c>false</c>.
        ''' </value>
        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return m_isShared
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return m_containingType.GetLexicalSortKey()
        End Function

        ''' <summary>
        ''' A potentially empty collection of locations that correspond to this instance.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_containingType.Locations
            End Get
        End Property

        ''' <summary>
        ''' Gets what kind of method this is. There are several different kinds of things in the
        ''' VB language that are represented as methods. This property allow distinguishing those things
        ''' without having to decode the name of the method.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        ''' <summary>
        ''' The parameters forming part of this signature.
        ''' </summary>
        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return ImmutableArray(Of ParameterSymbol).Empty
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return m_SyntaxNode
            End Get
        End Property

    End Class

End Namespace
