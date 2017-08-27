' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class MultilineLambdaStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of MultiLineLambdaExpressionSyntax)

        Protected Overrides Sub CollectBlockSpans(lambdaExpression As MultiLineLambdaExpressionSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            If Not lambdaExpression.EndSubOrFunctionStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    lambdaExpression, bannerNode:=lambdaExpression.SubOrFunctionHeader, autoCollapse:=False,
                    type:=BlockTypes.Expression, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
