' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class EnumDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of EnumStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(enumDeclaration As EnumStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(enumDeclaration, spans)

            Dim block = TryCast(enumDeclaration.Parent, EnumBlockSyntax)
            If Not block?.EndEnumStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=enumDeclaration, autoCollapse:=True))

                CollectCommentsRegions(block.EndEnumStatement, spans)
            End If
        End Sub
    End Class
End Namespace
