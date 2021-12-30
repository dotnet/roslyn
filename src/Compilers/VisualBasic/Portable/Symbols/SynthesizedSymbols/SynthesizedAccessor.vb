' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend MustInherit Class SynthesizedAccessor(Of T As Symbol)
        Inherits SynthesizedMethodBase

        Protected ReadOnly m_propertyOrEvent As T
        Private _lazyMetadataName As String

        Protected Sub New(container As NamedTypeSymbol, propertyOrEvent As T)
            MyBase.New(container)
            m_propertyOrEvent = propertyOrEvent
        End Sub

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return Binder.GetAccessorName(m_propertyOrEvent.Name, Me.MethodKind, Me.IsCompilationOutputWinMdObj())
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                If _lazyMetadataName Is Nothing Then
                    Interlocked.CompareExchange(_lazyMetadataName, GenerateMetadataName(), Nothing)
                End If
                Return _lazyMetadataName
            End Get
        End Property

        Protected Overridable Function GenerateMetadataName() As String
            ' VB compiler uses different rules for accessors that other members or the associated properties
            ' (probably a bug, but we have to maintain binary compatibility now). An accessor name is set to match
            ' its overridden method, regardless of what happens to its associated property.
            Dim overriddenMethod = Me.OverriddenMethod
            If overriddenMethod IsNot Nothing Then
                Return overriddenMethod.MetadataName
            Else
                Return Me.Name
            End If
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return m_propertyOrEvent
            End Get
        End Property

        Public ReadOnly Property PropertyOrEvent As T
            Get
                Return m_propertyOrEvent
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return m_propertyOrEvent.DeclaredAccessibility
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return m_propertyOrEvent.IsMustOverride
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return m_propertyOrEvent.IsNotOverridable
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return m_propertyOrEvent.IsOverloads
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return m_propertyOrEvent.ShadowsExplicitly
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return m_propertyOrEvent.IsOverridable
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return m_propertyOrEvent.IsOverrides
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return m_propertyOrEvent.IsShared
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return m_propertyOrEvent.GetLexicalSortKey()
        End Function

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_propertyOrEvent.Locations
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                Return m_propertyOrEvent
            End Get
        End Property

    End Class

End Namespace
