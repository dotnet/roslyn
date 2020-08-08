' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    '''  Structure containing all semantic information about an Await expression.
    ''' </summary>
    Public Structure AwaitExpressionInfo

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AwaitExpressionInfo" /> structure.
        ''' </summary>
        Friend Sub New(getAwaiter As IMethodSymbol, isCompleted As IPropertySymbol, getResult As IMethodSymbol)
            _getAwaiter = getAwaiter
            _isCompleted = isCompleted
            _getResult = getResult
        End Sub

        Private ReadOnly _getAwaiter As IMethodSymbol
        Private ReadOnly _isCompleted As IPropertySymbol
        Private ReadOnly _getResult As IMethodSymbol

        ''' <summary>
        ''' Gets the &quot;GetAwaiter&quot; method.
        ''' </summary>
        Public ReadOnly Property GetAwaiterMethod As IMethodSymbol
            Get
                Return _getAwaiter
            End Get
        End Property

        ''' <summary>
        ''' Gets the &quot;GetResult&quot; method.
        ''' </summary>
        Public ReadOnly Property GetResultMethod As IMethodSymbol
            Get
                Return _getResult
            End Get
        End Property

        ''' <summary>
        ''' Gets the &quot;IsCompleted&quot; property.
        ''' </summary>
        Public ReadOnly Property IsCompletedProperty As IPropertySymbol
            Get
                Return _isCompleted
            End Get
        End Property
    End Structure
End Namespace
