' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Inherits SubstitutableTypeParameterSymbol

        Private ReadOnly _typeMapFactory As Func(Of Symbol, TypeSubstitution)
        Private ReadOnly _container As Symbol
        Private ReadOnly _correspondingMethodTypeParameter As TypeParameterSymbol
        Private ReadOnly _name As String

        ' cannot use original constraints, etc. since they may refer to original type parameters
        Private _lazyConstraints As ImmutableArray(Of TypeSymbol)

        Friend Shared Function MakeTypeParameters(origParameters As ImmutableArray(Of TypeParameterSymbol), container As Symbol,
                                                  mapFunction As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol)) As ImmutableArray(Of TypeParameterSymbol)
            Return origParameters.SelectAsArray(mapFunction, container)
        End Function

        Friend Sub New(correspondingMethodTypeParameter As TypeParameterSymbol, container As Symbol, name As String, typeMapFactory As Func(Of Symbol, TypeSubstitution))
            Debug.Assert(correspondingMethodTypeParameter.IsDefinition)
            Debug.Assert(correspondingMethodTypeParameter.ContainingSymbol <> container)

            _container = container
            _correspondingMethodTypeParameter = correspondingMethodTypeParameter
            _name = name
            _typeMapFactory = typeMapFactory

            Debug.Assert(Me.TypeParameterKind = If(TypeOf Me.ContainingSymbol Is MethodSymbol, TypeParameterKind.Method,
                                                If(TypeOf Me.ContainingSymbol Is NamedTypeSymbol, TypeParameterKind.Type,
                                                TypeParameterKind.Cref)),
                $"Container is {Me.ContainingSymbol?.Kind}, TypeParameterKind is {Me.TypeParameterKind}")
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
                Return _typeMapFactory(Me._container)
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                If _lazyConstraints.IsDefault Then
                    Dim constraints = InternalSubstituteTypeParametersDistinct(TypeMap, _correspondingMethodTypeParameter.ConstraintTypesNoUseSiteDiagnostics)
                    ImmutableInterlocked.InterlockedInitialize(_lazyConstraints, constraints)
                End If
                Return _lazyConstraints
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return _correspondingMethodTypeParameter.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return _correspondingMethodTypeParameter.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return _correspondingMethodTypeParameter.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _correspondingMethodTypeParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _correspondingMethodTypeParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _correspondingMethodTypeParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return _correspondingMethodTypeParameter.Variance
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            _correspondingMethodTypeParameter.EnsureAllConstraintsAreResolved()
        End Sub

    End Class

End Namespace
