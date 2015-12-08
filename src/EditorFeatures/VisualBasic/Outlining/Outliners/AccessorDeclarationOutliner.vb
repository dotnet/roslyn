' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class AccessorDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of AccessorStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(accessorDeclaration As AccessorStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(accessorDeclaration, spans)

            Dim block = TryCast(accessorDeclaration.Parent, AccessorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=accessorDeclaration, autoCollapse:=True))
            End If
        End Sub
    End Class
End Namespace
