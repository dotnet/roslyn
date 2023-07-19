' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a member variable (field) that has undergone type substitution.
    ''' </summary>
    Friend NotInheritable Class SubstitutedFieldSymbol
        Inherits FieldSymbol

        Private ReadOnly _containingType As SubstitutedNamedType
        Private ReadOnly _originalDefinition As FieldSymbol

        Public Sub New(container As SubstitutedNamedType,
                       originalDefinition As FieldSymbol)
            Debug.Assert(originalDefinition.IsDefinition)
            _containingType = container
            _originalDefinition = originalDefinition
        End Sub

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

        Public Overrides ReadOnly Property Name As String
            Get
                Return _originalDefinition.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _originalDefinition.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _originalDefinition.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return _originalDefinition.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return _originalDefinition.IsNotSerialized
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return _originalDefinition.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return _originalDefinition.TypeLayoutOffset
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me.OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalDefinition As FieldSymbol
            Get
                Return _originalDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return _originalDefinition.IsConst
            End Get
        End Property

        Public Overrides ReadOnly Property ConstantValue As Object
            Get
                Return _originalDefinition.ConstantValue
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            Return _originalDefinition.GetConstantValue(inProgress)
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Dim underlying = OriginalDefinition.AssociatedSymbol
                Return If(underlying Is Nothing, Nothing, underlying.AsMember(ContainingType))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _originalDefinition.DeclaredAccessibility
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            ' Attributes do not undergo substitution
            Return _originalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _originalDefinition.IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _originalDefinition.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _originalDefinition.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _originalDefinition.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _originalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _originalDefinition.Type.InternalSubstituteTypeParameters(_containingType.TypeSubstitution).Type
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _containingType.TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.Type, _originalDefinition.CustomModifiers)
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Dim _hash As Integer = _originalDefinition.GetHashCode()
            Return Hash.Combine(_containingType, _hash)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, SubstitutedFieldSymbol)

            If other Is Nothing Then
                Return False
            End If

            If Not _originalDefinition.Equals(other._originalDefinition) Then
                Return False
            End If

            If Not _containingType.Equals(other._containingType) Then
                Return False
            End If

            Return True
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Public Overrides ReadOnly Property IsRequired As Boolean
            Get
                Return _originalDefinition.IsRequired
            End Get
        End Property
    End Class

End Namespace
