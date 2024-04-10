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
    ''' Represents a property that has undergone type substitution.
    ''' </summary>
    Friend NotInheritable Class SubstitutedPropertySymbol
        Inherits PropertySymbol

        Private ReadOnly _containingType As SubstitutedNamedType
        Private ReadOnly _originalDefinition As PropertySymbol
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _getMethod As SubstitutedMethodSymbol
        Private ReadOnly _setMethod As SubstitutedMethodSymbol
        Private ReadOnly _associatedField As SubstitutedFieldSymbol

        Public Sub New(container As SubstitutedNamedType,
                       originalDefinition As PropertySymbol,
                       getMethod As SubstitutedMethodSymbol,
                       setMethod As SubstitutedMethodSymbol,
                       associatedField As SubstitutedFieldSymbol)
            Debug.Assert(originalDefinition.IsDefinition)

            _containingType = container
            _originalDefinition = originalDefinition
            _parameters = SubstituteParameters()
            _getMethod = getMethod
            _setMethod = setMethod
            _associatedField = associatedField

            If _getMethod IsNot Nothing Then
                _getMethod.SetAssociatedPropertyOrEvent(Me)
            End If
            If _setMethod IsNot Nothing Then
                _setMethod.SetAssociatedPropertyOrEvent(Me)
            End If
        End Sub

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

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me.OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalDefinition As PropertySymbol
            Get
                Return _originalDefinition
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

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                Return _originalDefinition.IsWithEvents
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _originalDefinition.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return ImplementsHelper.SubstituteExplicitInterfaceImplementations(
                    _originalDefinition.ExplicitInterfaceImplementations,
                    TypeSubstitution)
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            ' Attributes do not undergo substitution
            Return _originalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return _getMethod
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return _setMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return _associatedField
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Return _originalDefinition.IsDefault
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return _originalDefinition.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return _originalDefinition.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _originalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return _originalDefinition.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _originalDefinition.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _originalDefinition.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return _originalDefinition.IsOverloads
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

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return _originalDefinition.ParameterCount
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _originalDefinition.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _originalDefinition.Type.InternalSubstituteTypeParameters(TypeSubstitution).Type
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.Type, _originalDefinition.TypeCustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.RefCustomModifiers)
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return _originalDefinition.CallingConvention
            End Get
        End Property

        Friend ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return _containingType.TypeSubstitution
            End Get
        End Property

        ' Create substituted version of all the parameters
        Private Function SubstituteParameters() As ImmutableArray(Of ParameterSymbol)

            Dim unsubstituted = _originalDefinition.Parameters
            Dim count = unsubstituted.Length

            If count = 0 Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            Else
                Dim substituted As ParameterSymbol() = New ParameterSymbol(count - 1) {}

                For i = 0 To count - 1
                    substituted(i) = SubstitutedParameterSymbol.CreatePropertyParameter(Me, unsubstituted(i))
                Next

                Return substituted.AsImmutableOrNull()
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim _hash As Integer = _originalDefinition.GetHashCode()
            Return Hash.Combine(_containingType, _hash)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, SubstitutedPropertySymbol)

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

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Debug.Assert(Not _originalDefinition.IsMyGroupCollectionProperty) ' the MyGroupCollection is not generic
                Return False
            End Get
        End Property

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
