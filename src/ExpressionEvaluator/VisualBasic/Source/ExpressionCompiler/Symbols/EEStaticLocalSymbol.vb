﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class EEStaticLocalSymbol
        Inherits EELocalSymbolBase

        Private ReadOnly _field As FieldSymbol
        Private ReadOnly _name As String

        Public Sub New(
            method As MethodSymbol,
            field As FieldSymbol,
            name As String)

            MyBase.New(method, field.Type)

            Debug.Assert(Not field.ContainingType.IsGenericType)

            _field = field
            _name = name
        End Sub

        Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
            Get
                Return LocalDeclarationKind.Static
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As EELocalSymbolBase
            Dim type = typeMap.SubstituteType(Me.Type)
            Debug.Assert(type Is Me.Type) ' containing type is not generic
            Return New EEStaticLocalSymbol(method, _field, _name)
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierToken As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierLocation As Location
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return NoLocations
            End Get
        End Property

        Friend Function ToBoundExpression(
            meParameter As BoundExpression,
            syntax As SyntaxNode,
            isLValue As Boolean) As BoundExpression

            Debug.Assert((meParameter Is Nothing) = _field.IsShared)
            Return New BoundFieldAccess(syntax, meParameter, _field, isLValue:=isLValue, type:=_field.Type)
        End Function
    End Class

End Namespace

