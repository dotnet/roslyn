' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class AccessorDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of AccessorStatementSyntax)

        Private Shared Function GetBannerText(accessorDeclaration As AccessorStatementSyntax) As String
            Dim builder As New BannerTextBuilder()

            For Each modifier In accessorDeclaration.Modifiers
                builder.Append(modifier.ToString())
                builder.Append(" "c)
            Next

            builder.Append(accessorDeclaration.DeclarationKeyword.ToString())
            builder.AppendParameterList(accessorDeclaration.ParameterList, emptyParentheses:=False)

            builder.Append(" "c)
            builder.Append(Ellipsis)

            Return builder.ToString()
        End Function

        Protected Overrides Sub CollectOutliningSpans(accessorDeclaration As AccessorStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(accessorDeclaration, spans)

            Dim methodBlock = TryCast(accessorDeclaration.Parent, AccessorBlockSyntax)
            If methodBlock IsNot Nothing Then
                If Not methodBlock.EndBlockStatement.IsMissing Then
                    spans.Add(
                    VisualBasicOutliningHelpers.CreateRegionFromBlock(
                        methodBlock,
                        GetBannerText(accessorDeclaration),
                        autoCollapse:=True))
                End If
            End If
        End Sub
    End Class
End Namespace
