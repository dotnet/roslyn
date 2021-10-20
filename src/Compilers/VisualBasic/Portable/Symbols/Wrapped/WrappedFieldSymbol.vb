' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a field that is based on another field.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedFieldSymbol
        Inherits FieldSymbol

        Protected _underlyingField As FieldSymbol

        Public ReadOnly Property UnderlyingField As FieldSymbol
            Get
                Return Me._underlyingField
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me._underlyingField.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me._underlyingField.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._underlyingField.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me._underlyingField.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Me._underlyingField.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return Me._underlyingField.IsNotSerialized
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return Me._underlyingField.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me._underlyingField.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return Me._underlyingField.MarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return Me._underlyingField.TypeLayoutOffset
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return Me._underlyingField.IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return Me._underlyingField.IsConst
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me._underlyingField.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property ConstantValue As Object
            Get
                Return Me._underlyingField.ConstantValue
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._underlyingField.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me._underlyingField.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me._underlyingField.IsShared
            End Get
        End Property

        Public Sub New(underlyingField As FieldSymbol)
            Debug.Assert(underlyingField IsNot Nothing)
            Me._underlyingField = underlyingField
        End Sub

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return Me._underlyingField.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            Return Me._underlyingField.GetConstantValue(inProgress)
        End Function
    End Class
End Namespace
