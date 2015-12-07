' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class TypeDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of TypeStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(typeDeclaration As TypeStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(typeDeclaration, spans)

            Dim typeBlock = TryCast(typeDeclaration.Parent, TypeBlockSyntax)
            If typeBlock IsNot Nothing Then
                If Not typeBlock.EndBlockStatement.IsMissing Then

                    spans.Add(
                            VisualBasicOutliningHelpers.CreateRegionFromBlock(
                                typeBlock,
                                typeDeclaration.ConvertToSingleLine().ToString() & " " & Ellipsis,
                                autoCollapse:=False))

                    VisualBasicOutliningHelpers.CollectCommentsRegions(typeBlock.EndBlockStatement, spans)
                End If
            End If
        End Sub
    End Class
End Namespace
