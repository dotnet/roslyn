' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class InterpolatedStringExpressionStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of InterpolatedStringExpressionSyntax)

        Protected Overrides Sub CollectBlockSpans(node As InterpolatedStringExpressionSyntax, spans As ArrayBuilder(Of BlockSpan), options As OptionSet, cancellationToken As CancellationToken)
            If node.DollarSignDoubleQuoteToken.IsMissing OrElse
               node.DoubleQuoteToken.IsMissing Then
                Return
            End If

            spans.Add(New BlockSpan(
                type:=BlockTypes.Expression,
                isCollapsible:=True,
                textSpan:=node.Span,
                autoCollapse:=True,
                isDefaultCollapsed:=False))
        End Sub
    End Class
End Namespace
