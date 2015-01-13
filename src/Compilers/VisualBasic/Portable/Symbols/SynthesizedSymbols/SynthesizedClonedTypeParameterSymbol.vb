' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Type parameter that represents another type parameter while being applied on a different symbol
    ''' </summary>
    Friend NotInheritable Class SynthesizedClonedTypeParameterSymbol
        Inherits TypeParameterSymbol

        Private ReadOnly m_typeMapFactory As Func(Of Symbol, TypeSubstitution)
        Private ReadOnly m_container As Symbol
        Private ReadOnly m_correspondingMethodTypeParameter As TypeParameterSymbol
        Private ReadOnly m_name As String

        ' cannot use original constraints, etc. since they may refer to original type parameters
        Private m_lazyConstraints As ImmutableArray(Of TypeSymbol)

        Friend Shared Function MakeTypeParameters(origParameters As ImmutableArray(Of TypeParameterSymbol), container As Symbol,
                                                  mapFunction As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol)) As ImmutableArray(Of TypeParameterSymbol)
            Return origParameters.SelectAsArray(mapFunction, container)
        End Function

        Friend Sub New(correspondingMethodTypeParameter As TypeParameterSymbol, container As Symbol, name As String, typeMapFactory As Func(Of Symbol, TypeSubstitution))
            Debug.Assert(correspondingMethodTypeParameter.IsDefinition)
            Debug.Assert(correspondingMethodTypeParameter.ContainingSymbol <> container)

            m_container = container
            m_correspondingMethodTypeParameter = correspondingMethodTypeParameter
            m_name = name
            m_typeMapFactory = typeMapFactory
        End Sub

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return If(TypeOf Me.ContainingSymbol Is MethodSymbol,
                          TypeParameterKind.Method,
                          TypeParameterKind.Type)
            End Get
        End Property

        Private ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return m_typeMapFactory(Me.m_container)
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                If m_lazyConstraints.IsDefault Then
                    Dim constraints = InternalSubstituteTypeParametersDistinct(TypeMap, m_correspondingMethodTypeParameter.ConstraintTypesNoUseSiteDiagnostics)
                    ImmutableInterlocked.InterlockedInitialize(m_lazyConstraints, constraints)
                End If
                Return m_lazyConstraints
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_container
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return m_correspondingMethodTypeParameter.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return m_correspondingMethodTypeParameter.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return m_correspondingMethodTypeParameter.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_correspondingMethodTypeParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_correspondingMethodTypeParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_correspondingMethodTypeParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return m_correspondingMethodTypeParameter.Variance
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            m_correspondingMethodTypeParameter.EnsureAllConstraintsAreResolved()
        End Sub

    End Class

End Namespace
