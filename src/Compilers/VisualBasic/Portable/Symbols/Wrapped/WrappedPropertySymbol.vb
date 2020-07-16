' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a property that is based on another property.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedPropertySymbol
        Inherits PropertySymbol

        Protected _underlyingProperty As PropertySymbol

        Public ReadOnly Property UnderlyingProperty As PropertySymbol
            Get
                Return Me._underlyingProperty
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me._underlyingProperty.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return Me._underlyingProperty.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Return Me._underlyingProperty.IsDefault
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As CallingConvention
            Get
                Return Me._underlyingProperty.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._underlyingProperty.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me._underlyingProperty.HasSpecialName
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._underlyingProperty.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me._underlyingProperty.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me._underlyingProperty.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me._underlyingProperty.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return Me._underlyingProperty.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return Me._underlyingProperty.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return Me._underlyingProperty.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return Me._underlyingProperty.IsNotOverridable
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me._underlyingProperty.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return Me._underlyingProperty.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Me._underlyingProperty.HasRuntimeSpecialName
            End Get
        End Property

        Public Sub New(underlyingProperty As PropertySymbol)
            Debug.Assert(underlyingProperty IsNot Nothing)
            Me._underlyingProperty = underlyingProperty
        End Sub

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return Me._underlyingProperty.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
