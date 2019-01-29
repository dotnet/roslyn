' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class StringLiteralExpressionStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of LiteralExpressionSyntax)

        Protected Overrides Sub CollectBlockSpans(node As LiteralExpressionSyntax, spans As ArrayBuilder(Of BlockSpan), options As OptionSet, cancellationToken As CancellationToken)
            If node.IsKind(SyntaxKind.StringLiteralExpression) AndAlso
                Not node.ContainsDiagnostics Then
                spans.Add(New BlockSpan(
                          type:=BlockTypes.Expression,
                          isCollapsible:=True,
                          textSpan:=node.Span,
                          autoCollapse:=True,
                          isDefaultCollapsed:=False))
            End If
        End Sub
    End Class
End Namespace
