' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Threading

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a Lambda parameter for a LambdaSymbol.
    ''' </summary>
    Friend NotInheritable Class BoundLambdaParameterSymbol
        Inherits LambdaParameterSymbol

        Private _lambdaSymbol As LambdaSymbol
        Private ReadOnly _syntaxNode As SyntaxNode

        Public Sub New(
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            isByRef As Boolean,
            syntaxNode As SyntaxNode,
            location As Location
        )
            MyBase.New(name, ordinal, type, isByRef, location)
            _syntaxNode = syntaxNode
        End Sub

        Public ReadOnly Property Syntax As SyntaxNode
            Get
                Return _syntaxNode
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _lambdaSymbol
            End Get
        End Property

        Public Sub SetLambdaSymbol(lambda As LambdaSymbol)
            Debug.Assert(_lambdaSymbol Is Nothing AndAlso lambda IsNot Nothing)
            _lambdaSymbol = lambda
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            End If

            Dim symbol = TryCast(obj, BoundLambdaParameterSymbol)
            Return symbol IsNot Nothing AndAlso Equals(symbol._lambdaSymbol, Me._lambdaSymbol) AndAlso symbol.Ordinal = Me.Ordinal
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me._lambdaSymbol.GetHashCode(), Me.Ordinal)
        End Function

    End Class

End Namespace
