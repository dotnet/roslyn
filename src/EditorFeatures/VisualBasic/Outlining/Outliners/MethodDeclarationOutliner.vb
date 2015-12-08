' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class MethodDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of MethodStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(methodDeclaration As MethodStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            CollectCommentsRegions(methodDeclaration, spans)

            Dim block = TryCast(methodDeclaration.Parent, MethodBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=methodDeclaration, autoCollapse:=True))
            End If
        End Sub
    End Class
End Namespace
