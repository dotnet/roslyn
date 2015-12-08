' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class TypeDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of TypeStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(typeDeclaration As TypeStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(typeDeclaration, spans)

            Dim block = TryCast(typeDeclaration.Parent, TypeBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=typeDeclaration, autoCollapse:=False))

                CollectCommentsRegions(block.EndBlockStatement, spans)
            End If
        End Sub
    End Class
End Namespace
