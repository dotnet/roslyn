' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class MultilineLambdaStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of MultiLineLambdaExpressionSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  lambdaExpression As MultiLineLambdaExpressionSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            If Not lambdaExpression.EndSubOrFunctionStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    lambdaExpression, bannerNode:=lambdaExpression.SubOrFunctionHeader, autoCollapse:=False,
                    type:=BlockTypes.Expression, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
