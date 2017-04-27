' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Public ReadOnly Property UnderlyingField As FieldSymbol

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return UnderlyingField.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return UnderlyingField.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingField.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return UnderlyingField.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return UnderlyingField.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return UnderlyingField.IsNotSerialized
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return UnderlyingField.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return UnderlyingField.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return UnderlyingField.MarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return UnderlyingField.TypeLayoutOffset
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return UnderlyingField.IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return UnderlyingField.IsConst
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return UnderlyingField.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property ConstantValue As Object
            Get
                Return UnderlyingField.ConstantValue
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return UnderlyingField.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return UnderlyingField.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return UnderlyingField.IsShared
            End Get
        End Property

        Public Sub New(underlyingField As FieldSymbol)
            Debug.Assert(underlyingField IsNot Nothing)
            Me.UnderlyingField = underlyingField
        End Sub

        Public Overrides Function GetDocumentationCommentXml(
                                                     Optional preferredCulture As CultureInfo = Nothing,
                                                     Optional expandIncludes As Boolean = False,
                                                     Optional cancellationToken As CancellationToken = Nothing
                                                            ) As String
            Return UnderlyingField.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
            Return UnderlyingField.GetConstantValue(inProgress)
        End Function
    End Class
End Namespace
