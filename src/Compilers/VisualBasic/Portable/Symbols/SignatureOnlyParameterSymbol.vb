' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Private ReadOnly _type As TypeSymbol
        Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _refCustomModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _defaultValue As ConstantValue
        Private ReadOnly _isParamArray As Boolean
        Private ReadOnly _isByRef As Boolean
        Private ReadOnly _isOut As Boolean
        Private ReadOnly _isOptional As Boolean

        Public Sub New(ByVal type As TypeSymbol,
                       ByVal customModifiers As ImmutableArray(Of CustomModifier),
                       ByVal refCustomModifiers As ImmutableArray(Of CustomModifier),
                       ByVal defaultConstantValue As ConstantValue,
                       ByVal isParamArray As Boolean,
                       ByVal isByRef As Boolean,
                       ByVal isOut As Boolean,
                       ByVal isOptional As Boolean)

            Me._type = type
            Me._customModifiers = customModifiers
            Me._refCustomModifiers = refCustomModifiers
            Me._defaultValue = defaultConstantValue
            Me._isParamArray = isParamArray
            Me._isByRef = isByRef
            Me._isOut = isOut
            Me._isOptional = isOptional
        End Sub

        Public Overrides ReadOnly Property Type() As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers() As ImmutableArray(Of CustomModifier)
            Get
                Return _customModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers() As ImmutableArray(Of CustomModifier)
            Get
                Return _refCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return _isParamArray
            End Get
        End Property

        Public Overrides ReadOnly Property Name() As String
            Get
                Return ""
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return _isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return _isOut
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return Not _isOut
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional() As Boolean
            Get
                Return _isOptional
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue() As Boolean
            Get
                Return _defaultValue IsNot Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return _defaultValue
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

        Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property
#End Region
    End Class
End Namespace
