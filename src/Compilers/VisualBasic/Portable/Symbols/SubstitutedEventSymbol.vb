' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SubstitutedEventSymbol
        Inherits EventSymbol

        Private ReadOnly _originalDefinition As EventSymbol
        Private ReadOnly _containingType As SubstitutedNamedType

        Private ReadOnly _addMethod As SubstitutedMethodSymbol
        Private ReadOnly _removeMethod As SubstitutedMethodSymbol
        Private ReadOnly _raiseMethod As SubstitutedMethodSymbol

        Private ReadOnly _associatedField As SubstitutedFieldSymbol

        Private _lazyType As TypeSymbol

        'we want to compute this lazily since it may be expensive for the underlying symbol
        Private _lazyExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
        Private _lazyOverriddenOrHiddenMembers As OverriddenMembersResult(Of EventSymbol)


        Friend Sub New(containingType As SubstitutedNamedType,
                       originalDefinition As EventSymbol,
                       addMethod As SubstitutedMethodSymbol,
                       removeMethod As SubstitutedMethodSymbol,
                       raiseMethod As SubstitutedMethodSymbol,
                       associatedField As SubstitutedFieldSymbol)

            Me._containingType = containingType
            Me._originalDefinition = originalDefinition
            Me._associatedField = associatedField

            If addMethod IsNot Nothing Then
                addMethod.SetAssociatedPropertyOrEvent(Me)
                _addMethod = addMethod
            End If

            If removeMethod IsNot Nothing Then
                removeMethod.SetAssociatedPropertyOrEvent(Me)
                _removeMethod = removeMethod
            End If

            If raiseMethod IsNot Nothing Then
                raiseMethod.SetAssociatedPropertyOrEvent(Me)
                _raiseMethod = raiseMethod
            End If
        End Sub

        Friend ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return _containingType.TypeSubstitution
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                If Me._lazyType Is Nothing Then
                    Interlocked.CompareExchange(Me._lazyType,
                                                _originalDefinition.Type.InternalSubstituteTypeParameters(TypeSubstitution).AsTypeSymbolOnly(),
                                                Nothing)
                End If

                Return Me._lazyType
            End Get

        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me.OriginalDefinition.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me._originalDefinition.HasSpecialName
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalDefinition As EventSymbol
            Get
                Return Me._originalDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._originalDefinition.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me._originalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._originalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me._originalDefinition.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return Me._originalDefinition.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return Me._originalDefinition.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return Me._originalDefinition.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return Me.OriginalDefinition.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me.OriginalDefinition.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return _addMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return _removeMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return _raiseMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return _associatedField
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitInterfaceImplementation As Boolean
            Get
                Return Me._originalDefinition.IsExplicitInterfaceImplementation
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                If _lazyExplicitInterfaceImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_lazyExplicitInterfaceImplementations,
                                                        ImplementsHelper.SubstituteExplicitInterfaceImplementations(
                                                                                _originalDefinition.ExplicitInterfaceImplementations,
                                                                                TypeSubstitution),
                                                        Nothing)
                End If

                Return _lazyExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me.OriginalDefinition.DeclaredAccessibility
            End Get

        End Property

        Friend Overrides ReadOnly Property OverriddenOrHiddenMembers As OverriddenMembersResult(Of EventSymbol)

            Get
                If Me._lazyOverriddenOrHiddenMembers Is Nothing Then
                    Interlocked.CompareExchange(Me._lazyOverriddenOrHiddenMembers,
                                                OverrideHidingHelper(Of EventSymbol).MakeOverriddenMembers(Me),
                                                Nothing)
                End If

                Return Me._lazyOverriddenOrHiddenMembers
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me.OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Return _originalDefinition.IsWindowsRuntimeEvent
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
