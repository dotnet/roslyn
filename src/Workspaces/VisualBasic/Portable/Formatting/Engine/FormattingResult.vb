' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    ''' <summary>
    ''' this holds onto changes made by formatting engine.
    ''' 
    ''' currently it only has an ability to apply those changes to buffer. but it could be expanded to
    ''' support other cases as well such as tree or etc.
    ''' </summary>
    Friend Class FormattingResult
        Inherits AbstractFormattingResult
        Implements IFormattingResult

        Friend Sub New(treeInfo As TreeData, tokenStream As TokenStream, spanToFormat As TextSpan)
            MyBase.New(treeInfo, tokenStream, spanToFormat)
        End Sub

        Protected Overrides Function Rewriter(changeMap As Dictionary(Of ValueTuple(Of SyntaxToken, SyntaxToken), TriviaData), cancellationToken As CancellationToken) As SyntaxNode
            Dim triviaRewriter = New TriviaDataFactory.TriviaRewriter(Me.TreeInfo.Root, SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, Me.FormattedSpan), changeMap, cancellationToken)
            Return triviaRewriter.Transform()
        End Function
    End Class
End Namespace
