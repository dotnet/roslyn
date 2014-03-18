' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private ReadOnly m_originalDefinition As EventSymbol
        Private ReadOnly m_containingType As SubstitutedNamedType

        Private ReadOnly m_addMethod As SubstitutedMethodSymbol
        Private ReadOnly m_removeMethod As SubstitutedMethodSymbol
        Private ReadOnly m_raiseMethod As SubstitutedMethodSymbol

        Private ReadOnly m_associatedField As SubstitutedFieldSymbol

        Private lazyType As TypeSymbol

        'we want to compute this lazily since it may be expensive for the underlying symbol
        Private lazyExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
        Private lazyOverriddenOrHiddenMembers As OverriddenMembersResult(Of EventSymbol)


        Friend Sub New(containingType As SubstitutedNamedType,
                       originalDefinition As EventSymbol,
                       addMethod As SubstitutedMethodSymbol,
                       removeMethod As SubstitutedMethodSymbol,
                       raiseMethod As SubstitutedMethodSymbol,
                       associatedField As SubstitutedFieldSymbol)

            Me.m_containingType = containingType
            Me.m_originalDefinition = originalDefinition
            Me.m_associatedField = associatedField

            If addMethod IsNot Nothing Then
                addMethod.SetAssociatedPropertyOrEvent(Me)
                m_addMethod = addMethod
            End If

            If removeMethod IsNot Nothing Then
                removeMethod.SetAssociatedPropertyOrEvent(Me)
                m_removeMethod = removeMethod
            End If

            If raiseMethod IsNot Nothing Then
                raiseMethod.SetAssociatedPropertyOrEvent(Me)
                m_raiseMethod = raiseMethod
            End If
        End Sub

        Friend ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return m_containingType.TypeSubstitution
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                If Me.lazyType Is Nothing Then
                    Interlocked.CompareExchange(Me.lazyType,
                                                m_originalDefinition.Type.InternalSubstituteTypeParameters(TypeSubstitution),
                                                Nothing)
                End If

                Return Me.lazyType
            End Get

        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me.OriginalDefinition.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me.m_originalDefinition.HasSpecialName
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me.m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Me.m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalDefinition As EventSymbol
            Get
                Return Me.m_originalDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me.m_originalDefinition.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me.m_originalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.m_originalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me.m_originalDefinition.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return Me.m_originalDefinition.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return Me.m_originalDefinition.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return Me.m_originalDefinition.IsOverridable
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
                Return m_addMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return m_removeMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return m_raiseMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return m_associatedField
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitInterfaceImplementation As Boolean
            Get
                Return Me.m_originalDefinition.IsExplicitInterfaceImplementation
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                If lazyExplicitInterfaceImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(lazyExplicitInterfaceImplementations,
                                                        ImplementsHelper.SubstituteExplicitInterfaceImplementations(
                                                                                m_originalDefinition.ExplicitInterfaceImplementations,
                                                                                TypeSubstitution),
                                                        Nothing)
                End If

                Return lazyExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me.OriginalDefinition.DeclaredAccessibility
            End Get

        End Property

        Friend Overrides ReadOnly Property OverriddenOrHiddenMembers As OverriddenMembersResult(Of EventSymbol)

            Get
                If Me.lazyOverriddenOrHiddenMembers Is Nothing Then
                    Interlocked.CompareExchange(Me.lazyOverriddenOrHiddenMembers,
                                                OverrideHidingHelper(Of EventSymbol).MakeOverriddenMembers(Me),
                                                Nothing)
                End If

                Return Me.lazyOverriddenOrHiddenMembers
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me.OriginalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Return m_originalDefinition.IsWindowsRuntimeEvent
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return m_originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace