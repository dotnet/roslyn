' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private m_LambdaSymbol As LambdaSymbol
        Private ReadOnly m_SyntaxNode As VBSyntaxNode

        Public Sub New(
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            isByRef As Boolean,
            syntaxNode As VBSyntaxNode,
            location As Location
        )
            MyBase.New(name, ordinal, type, isByRef, location)
            m_SyntaxNode = syntaxNode
        End Sub

        Public ReadOnly Property Syntax As VBSyntaxNode
            Get
                Return m_SyntaxNode
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_LambdaSymbol
            End Get
        End Property

        Public Sub SetLambdaSymbol(lambda As LambdaSymbol)
            Debug.Assert(m_LambdaSymbol Is Nothing AndAlso lambda IsNot Nothing)
            m_LambdaSymbol = lambda
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            End If

            Dim symbol = TryCast(obj, BoundLambdaParameterSymbol)
            Return symbol IsNot Nothing AndAlso Equals(symbol.m_LambdaSymbol, Me.m_LambdaSymbol) AndAlso symbol.Ordinal = Me.Ordinal
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.m_LambdaSymbol.GetHashCode(), Me.Ordinal)
        End Function

    End Class

End Namespace