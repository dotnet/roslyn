﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class EETypeParameterSymbol
        Inherits TypeParameterSymbol

        Private ReadOnly _container As Symbol
        Private ReadOnly _sourceTypeParameterSymbol As TypeParameterSymbol
        Private ReadOnly _ordinal As Integer
        Private ReadOnly _getTypeParameterMap As Func(Of TypeSubstitution)

        Public Sub New(
            container As Symbol,
            sourceTypeParameterSymbol As TypeParameterSymbol,
            ordinal As Integer,
            getTypeParameterMap As Func(Of TypeSubstitution))

            Debug.Assert(container.Kind = SymbolKind.NamedType OrElse container.Kind = SymbolKind.Method)
            _container = container
            _sourceTypeParameterSymbol = sourceTypeParameterSymbol
            _ordinal = ordinal
            _getTypeParameterMap = getTypeParameterMap
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _sourceTypeParameterSymbol.GetUnmangledName()
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return _sourceTypeParameterSymbol.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return _sourceTypeParameterSymbol.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return _sourceTypeParameterSymbol.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return _sourceTypeParameterSymbol.Variance
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            _sourceTypeParameterSymbol.EnsureAllConstraintsAreResolved()
        End Sub

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Dim constraintTypes = _sourceTypeParameterSymbol.ConstraintTypesNoUseSiteDiagnostics
                If constraintTypes.IsEmpty Then
                    Return ImmutableArray(Of TypeSymbol).Empty
                End If

                ' Remap constraints from _sourceTypeParameterSymbol since constraints
                ' may be defined in terms of other type parameters.
                Dim substitution = _getTypeParameterMap()
                Debug.Assert(substitution IsNot Nothing, "Expected substitution to have been populated.")
                Return InternalSubstituteTypeParametersDistinct(substitution, constraintTypes)
            End Get
        End Property
    End Class
End Namespace
