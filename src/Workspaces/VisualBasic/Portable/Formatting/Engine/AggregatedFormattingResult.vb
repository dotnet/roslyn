﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class AggregatedFormattingResult
        Inherits AbstractAggregatedFormattingResult
        Implements IFormattingResult

        Public Sub New(node As SyntaxNode, results As IList(Of AbstractFormattingResult), formattingSpans As SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector))
            MyBase.New(node, results, formattingSpans)
        End Sub

        Protected Overrides Function Rewriter(changeMap As Dictionary(Of ValueTuple(Of SyntaxToken, SyntaxToken), TriviaData), cancellationToken As CancellationToken) As SyntaxNode
            Dim triviaRewriter =
                New TriviaDataFactory.TriviaRewriter(Me.Node, GetFormattingSpans(), changeMap, cancellationToken)
            Return triviaRewriter.Transform()
        End Function
    End Class
End Namespace
