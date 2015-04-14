' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class MultilineLambdaOutliner
        Inherits AbstractSyntaxNodeOutliner(Of MultiLineLambdaExpressionSyntax)

        Private Shared Function GetBannerText(lambdaHeader As LambdaHeaderSyntax) As String
            Dim builder As New BannerTextBuilder()
            For Each modifier In lambdaHeader.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            builder.Append(lambdaHeader.DeclarationKeyword.ToString())

            builder.AppendParameterList(lambdaHeader.ParameterList, emptyParentheses:=True)
            builder.AppendAsClause(lambdaHeader.AsClause)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(lambdaExpression As MultiLineLambdaExpressionSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            If lambdaExpression.EndSubOrFunctionStatement.IsMissing Then
                Return
            End If

            spans.Add(
                VisualBasicOutliningHelpers.CreateRegionFromBlock(
                    lambdaExpression,
                    GetBannerText(lambdaExpression.SubOrFunctionHeader),
                    autoCollapse:=False))
        End Sub
    End Class
End Namespace
