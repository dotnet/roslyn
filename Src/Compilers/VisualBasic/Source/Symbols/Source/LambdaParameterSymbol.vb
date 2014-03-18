' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Threading

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a Lambda parameter.
    ''' </summary>
    Friend MustInherit Class LambdaParameterSymbol
        Inherits ParameterSymbol

        Private ReadOnly m_location As ImmutableArray(Of Location)
        Private ReadOnly m_name As String
        Private ReadOnly m_type As TypeSymbol ' Can be Nothing for UnboundLambdaParameterSymbol.
        Private ReadOnly m_ordinal As UShort
        Private ReadOnly m_isByRef As Boolean

        Protected Sub New(
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            isByRef As Boolean,
            location As Location
        )
            m_name = name
            m_ordinal = CType(ordinal, UShort)
            m_type = type

            If location IsNot Nothing Then
                m_location = ImmutableArray.Create(Of Location)(location)
            Else
                m_location = ImmutableArray(Of Location).Empty
            End If

            m_isByRef = isByRef
        End Sub

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_ordinal
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return Nothing
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
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

        Friend NotOverridable Overrides ReadOnly Property HasByRefBeforeCustomModifiers As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_location
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(Of ParameterSyntax)(Me.Locations)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return m_type
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return m_isByRef
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return m_isByRef
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

    End Class

End Namespace