' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class MeParameterSymbol
        Inherits ParameterSymbol

        Private ReadOnly _container As Symbol
        Private ReadOnly _type As TypeSymbol

        Friend Sub New(memberSymbol As Symbol)
            Me._container = memberSymbol
            Me._type = _container.ContainingType
        End Sub

        Friend Sub New(memberSymbol As Symbol, type As TypeSymbol)
            Me._container = memberSymbol
            Me._type = type
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Me"
            End Get
        End Property

        Public Overrides ReadOnly Property [Type] As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return If(_container Is Nothing, ImmutableArray(Of Location).Empty, _container.Locations)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return -1
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return Type.IsValueType
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                Return -1
            End Get
        End Property

        Public Overrides ReadOnly Property IsMe As Boolean
            Get
                Return True
            End Get
        End Property

        ' "Me" is implicitly declared.
        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

    End Class

End Namespace
