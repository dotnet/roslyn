' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' this is a SyntaxReference implementation that lazily translates the result (SyntaxNode) of the original syntax reference
    ''' to other one.
    ''' </summary>
    Friend Class TranslationSyntaxReference
        Inherits SyntaxReference

        Private ReadOnly _reference As SyntaxReference
        Private ReadOnly _nodeGetter As Func(Of SyntaxReference, SyntaxNode)

        Public Sub New(reference As SyntaxReference, nodeGetter As Func(Of SyntaxReference, SyntaxNode))
            _reference = reference
            _nodeGetter = nodeGetter
        End Sub

        Public Overrides Function GetSyntax(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
            Dim node = _nodeGetter(_reference)
            Debug.Assert(node.SyntaxTree Is _reference.SyntaxTree)
            Return node
        End Function

        Public Overrides ReadOnly Property Span As TextSpan
            Get
                Return _reference.Span
            End Get
        End Property

        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _reference.SyntaxTree
            End Get
        End Property
    End Class
End Namespace
