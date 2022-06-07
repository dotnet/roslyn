' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents an event that is based on another event.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedEventSymbol
        Inherits EventSymbol

        Protected ReadOnly _underlyingEvent As EventSymbol

        Public ReadOnly Property UnderlyingEvent As EventSymbol
            Get
                Return Me._underlyingEvent
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me._underlyingEvent.IsImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me._underlyingEvent.HasSpecialName
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._underlyingEvent.Name
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._underlyingEvent.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me._underlyingEvent.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me._underlyingEvent.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me._underlyingEvent.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return Me._underlyingEvent.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return Me._underlyingEvent.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return Me._underlyingEvent.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return Me._underlyingEvent.IsNotOverridable
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me._underlyingEvent.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Return Me._underlyingEvent.IsWindowsRuntimeEvent
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Me._underlyingEvent.HasRuntimeSpecialName
            End Get
        End Property

        Public Sub New(underlyingEvent As EventSymbol)
            Debug.Assert(underlyingEvent IsNot Nothing)
            Me._underlyingEvent = underlyingEvent
        End Sub

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return Me._underlyingEvent.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

