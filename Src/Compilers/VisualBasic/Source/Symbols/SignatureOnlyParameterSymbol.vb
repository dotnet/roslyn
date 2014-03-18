' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Intended to be used to create ParameterSymbols for a SignatureOnlyMethodSymbol.
    ''' </summary>
    Friend NotInheritable Class SignatureOnlyParameterSymbol
        Inherits ParameterSymbol
        Private ReadOnly m_type As TypeSymbol
        Private ReadOnly m_customModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly m_defaultValue As ConstantValue
        Private ReadOnly m_isParamArray As Boolean
        Private ReadOnly m_isByRef As Boolean
        Private ReadOnly m_isOut As Boolean
        Private ReadOnly m_isOptional As Boolean

        Public Sub New(ByVal type As TypeSymbol,
                       ByVal customModifiers As ImmutableArray(Of CustomModifier),
                       ByVal defaultConstantValue As ConstantValue,
                       ByVal isParamArray As Boolean,
                       ByVal isByRef As Boolean,
                       ByVal isOut As Boolean,
                       ByVal isOptional As Boolean)

            Me.m_type = type
            Me.m_customModifiers = customModifiers
            Me.m_defaultValue = defaultConstantValue
            Me.m_isParamArray = isParamArray
            Me.m_isByRef = isByRef
            Me.m_isOut = isOut
            Me.m_isOptional = isOptional
        End Sub

        Public Overrides ReadOnly Property Type() As TypeSymbol
            Get
                Return m_type
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers() As ImmutableArray(Of CustomModifier)
            Get
                Return m_customModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return m_isParamArray
            End Get
        End Property

        Public Overrides ReadOnly Property Name() As String
            Get
                Return ""
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return m_isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return m_isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return m_isOut
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return Not m_isOut
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional() As Boolean
            Get
                Return m_isOptional
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue() As Boolean
            Get
                Return m_defaultValue IsNot Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return m_defaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return False
            End Get
        End Property

#Region "Not used by MethodSignatureComparer"
        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal() As Integer
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol() As Symbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations() As ImmutableArray(Of Location)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly() As AssemblySymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property HasByRefBeforeCustomModifiers As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property
#End Region

    End Class
End Namespace
