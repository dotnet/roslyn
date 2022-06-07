' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a parameter that has undergone type substitution.
    ''' </summary>
    Friend MustInherit Class SubstitutedParameterSymbol
        Inherits ParameterSymbol

        Private ReadOnly _originalDefinition As ParameterSymbol

        Public Shared Function CreateMethodParameter(container As SubstitutedMethodSymbol, originalDefinition As ParameterSymbol) As SubstitutedParameterSymbol
            Return New SubstitutedMethodParameterSymbol(container, originalDefinition)
        End Function

        Public Shared Function CreatePropertyParameter(container As SubstitutedPropertySymbol, originalDefinition As ParameterSymbol) As SubstitutedParameterSymbol
            Return New SubstitutedPropertyParameterSymbol(container, originalDefinition)
        End Function

        Protected Sub New(originalDefinition As ParameterSymbol)
            Debug.Assert(originalDefinition.IsDefinition)
            _originalDefinition = originalDefinition
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

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _originalDefinition.Ordinal
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property ContainingSymbol As Symbol

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return _originalDefinition.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return _originalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return _originalDefinition.HasExplicitDefaultValue
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return _originalDefinition.IsOptional
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return _originalDefinition.IsParamArray
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
                Return _originalDefinition.Type.InternalSubstituteTypeParameters(TypeSubstitution).Type
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _originalDefinition.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return _originalDefinition.IsMetadataOut
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return _originalDefinition.IsMetadataIn
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return _originalDefinition.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return _originalDefinition.HasOptionCompare
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return _originalDefinition.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return _originalDefinition.IsIUnknownConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return _originalDefinition.IsCallerLineNumber
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return _originalDefinition.IsCallerMemberName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return _originalDefinition.IsCallerFilePath
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                Return _originalDefinition.CallerArgumentExpressionParameterIndex
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.RefCustomModifiers)
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return _originalDefinition.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.Type, _originalDefinition.CustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalDefinition As ParameterSymbol
            Get
                Return _originalDefinition
            End Get
        End Property

        Protected MustOverride ReadOnly Property TypeSubstitution As TypeSubstitution

        Public Overrides Function GetHashCode() As Integer
            Dim _hash As Integer = _originalDefinition.GetHashCode()
            Return Hash.Combine(ContainingSymbol, _hash)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, SubstitutedParameterSymbol)
            If other Is Nothing Then
                Return False
            End If

            If Not _originalDefinition.Equals(other._originalDefinition) Then
                Return False
            End If

            If Not ContainingSymbol.Equals(other.ContainingSymbol) Then
                Return False
            End If

            Return True
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        ''' <summary>
        ''' Represents a method parameter that has undergone type substitution.
        ''' </summary>
        Friend Class SubstitutedMethodParameterSymbol
            Inherits SubstitutedParameterSymbol

            Private ReadOnly _container As SubstitutedMethodSymbol

            Public Sub New(container As SubstitutedMethodSymbol,
                           originalDefinition As ParameterSymbol)
                MyBase.New(originalDefinition)
                _container = container
            End Sub

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Protected Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    Return _container.TypeSubstitution
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Represents a property parameter that has undergone type substitution.
        ''' </summary>
        Private NotInheritable Class SubstitutedPropertyParameterSymbol
            Inherits SubstitutedParameterSymbol

            Private ReadOnly _container As SubstitutedPropertySymbol

            Public Sub New(container As SubstitutedPropertySymbol,
                           originalDefinition As ParameterSymbol)
                MyBase.New(originalDefinition)
                _container = container
            End Sub

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Protected Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
                Get
                    Return _container.TypeSubstitution
                End Get
            End Property
        End Class

    End Class

End Namespace
