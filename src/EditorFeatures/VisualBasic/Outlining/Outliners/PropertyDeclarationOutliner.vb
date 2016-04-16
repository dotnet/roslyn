' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class PropertyDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of PropertyStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(propertyDeclaration As PropertyStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(propertyDeclaration, spans)

            Dim block = TryCast(propertyDeclaration.Parent, PropertyBlockSyntax)
            If Not block?.EndPropertyStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=propertyDeclaration, autoCollapse:=True))

                CollectCommentsRegions(block.EndPropertyStatement, spans)
            End If
        End Sub
    End Class
End Namespace
