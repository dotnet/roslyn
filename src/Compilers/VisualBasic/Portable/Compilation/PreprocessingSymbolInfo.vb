' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Structure VisualBasicPreprocessingSymbolInfo
        Implements IEquatable(Of VisualBasicPreprocessingSymbolInfo)

        Private ReadOnly _symbol As PreprocessingSymbol
        Private ReadOnly _constantValue As Object
        Private ReadOnly _isDefined As Boolean

        Friend Shared None As New VisualBasicPreprocessingSymbolInfo(Nothing, Nothing, False)

        ''' <summary>
        ''' The symbol that was referred to by the identifier, if any. 
        ''' </summary>
        Public ReadOnly Property Symbol As PreprocessingSymbol
            Get
                Return _symbol
            End Get
        End Property

        Public ReadOnly Property IsDefined As Boolean
            Get
                Return _isDefined
            End Get
        End Property

        ''' <summary>
        ''' Returns the constant value associated with the symbol, if any.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property ConstantValue As Object
            Get
                Return _constantValue
            End Get
        End Property

        Public Shared Widening Operator CType(info As VisualBasicPreprocessingSymbolInfo) As PreprocessingSymbolInfo
            Return New PreprocessingSymbolInfo(info.Symbol, info.IsDefined)
        End Operator

        Friend Sub New(symbol As PreprocessingSymbol, constantValueOpt As Object, isDefined As Boolean)
            Me._symbol = symbol
            Me._constantValue = constantValueOpt
            Me._isDefined = isDefined
        End Sub

        Public Overloads Function Equals(other As VisualBasicPreprocessingSymbolInfo) As Boolean Implements IEquatable(Of VisualBasicPreprocessingSymbolInfo).Equals
            Return _isDefined = other._isDefined AndAlso _symbol = other._symbol AndAlso
                Equals(Me._constantValue, other._constantValue)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is VisualBasicTypeInfo AndAlso
                Equals(DirectCast(obj, VisualBasicTypeInfo))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_symbol, Hash.Combine(_constantValue, CInt(_isDefined)))
        End Function

    End Structure
End Namespace
