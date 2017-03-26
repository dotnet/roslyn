' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Public ReadOnly Property UnderlyingEvent As EventSymbol

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return UnderlyingEvent.IsImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return UnderlyingEvent.HasSpecialName
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingEvent.Name
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return UnderlyingEvent.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return UnderlyingEvent.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return UnderlyingEvent.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return UnderlyingEvent.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return UnderlyingEvent.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return UnderlyingEvent.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return UnderlyingEvent.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return UnderlyingEvent.IsNotOverridable
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return UnderlyingEvent.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Return UnderlyingEvent.IsWindowsRuntimeEvent
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return UnderlyingEvent.HasRuntimeSpecialName
            End Get
        End Property

        Public Sub New(underlyingEvent As EventSymbol)
            Debug.Assert(underlyingEvent IsNot Nothing)
            Me.underlyingEvent = underlyingEvent
        End Sub

        Public Overrides Function GetDocumentationCommentXml(
                                                     Optional preferredCulture As CultureInfo = Nothing,
                                                     Optional expandIncludes As Boolean = False,
                                                     Optional cancellationToken As CancellationToken = Nothing
                                                            ) As String
            Return UnderlyingEvent.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

