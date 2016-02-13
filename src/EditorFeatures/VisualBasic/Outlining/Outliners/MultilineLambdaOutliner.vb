' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class MultilineLambdaOutliner
        Inherits AbstractSyntaxNodeOutliner(Of MultiLineLambdaExpressionSyntax)

        Protected Overrides Sub CollectOutliningSpans(lambdaExpression As MultiLineLambdaExpressionSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            If Not lambdaExpression.EndSubOrFunctionStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(lambdaExpression, bannerNode:=lambdaExpression.SubOrFunctionHeader, autoCollapse:=False))
            End If
        End Sub
    End Class
End Namespace
