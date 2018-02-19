﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    ''' <summary>
    ''' A simple type parameter with no constraints.
    ''' </summary>
    Friend NotInheritable Class SimpleTypeParameterSymbol
        Inherits TypeParameterSymbol

        Private ReadOnly _container As Symbol
        Private ReadOnly _ordinal As Integer
        Private ReadOnly _name As String

        Public Sub New(container As Symbol, ordinal As Integer, name As String)
            _container = container
            _ordinal = ordinal
            _name = name
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return TypeParameterKind.Type
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return VarianceKind.None
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            Throw ExceptionUtilities.Unreachable
        End Sub
    End Class
End Namespace
