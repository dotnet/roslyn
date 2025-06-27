' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class EELocalSymbol
        Inherits EELocalSymbolBase

        Private ReadOnly _declarationKind As LocalDeclarationKind
        Private ReadOnly _locations As ImmutableArray(Of Location)
        Private ReadOnly _nameOpt As String
        Private ReadOnly _ordinal As Integer
        Private ReadOnly _isPinned As Boolean
        Private ReadOnly _isByRef As Boolean
        Private ReadOnly _canScheduleToStack As Boolean

        Public Sub New(
            method As MethodSymbol,
            locations As ImmutableArray(Of Location),
            nameOpt As String,
            ordinal As Integer,
            declarationKind As LocalDeclarationKind,
            type As TypeSymbol,
            isByRef As Boolean,
            isPinned As Boolean,
            canScheduleToStack As Boolean)

            MyBase.New(method, type)

            Debug.Assert(method IsNot Nothing)
            Debug.Assert(ordinal >= -1)
            Debug.Assert(Not locations.IsDefault)
            Debug.Assert(type IsNot Nothing)

            _nameOpt = nameOpt
            _ordinal = ordinal
            _locations = locations
            _isByRef = isByRef
            _isPinned = isPinned
            _canScheduleToStack = canScheduleToStack
            _declarationKind = declarationKind
        End Sub

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As EELocalSymbolBase
            Dim type = typeMap.SubstituteType(Me.Type)
            Return New EELocalSymbol(method, _locations, _nameOpt, _ordinal, _declarationKind, type, _isByRef, _isPinned, _canScheduleToStack)
        End Function

        Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
            Get
                Return _declarationKind
            End Get
        End Property

        Friend Overrides ReadOnly Property CanScheduleToStack As Boolean
            Get
                Return _canScheduleToStack
            End Get
        End Property

        Friend ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _nameOpt
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
                Return _locations
            End Get
        End Property

        Friend Overrides Function GetConstantValue(binder As Binder) As ConstantValue
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsPinned As Boolean
            Get
                Return _isPinned
            End Get
        End Property
    End Class
End Namespace

