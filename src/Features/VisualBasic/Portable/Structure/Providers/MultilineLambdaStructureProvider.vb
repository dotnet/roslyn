' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class MultilineLambdaStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of MultiLineLambdaExpressionSyntax)

        Protected Overrides Sub CollectBlockSpans(lambdaExpression As MultiLineLambdaExpressionSyntax,
                                                  spans As ImmutableArray(Of BlockSpan).Builder,
                                                  cancellationToken As CancellationToken)
            If Not lambdaExpression.EndSubOrFunctionStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(lambdaExpression, bannerNode:=lambdaExpression.SubOrFunctionHeader, autoCollapse:=False))
            End If
        End Sub
    End Class
End Namespace