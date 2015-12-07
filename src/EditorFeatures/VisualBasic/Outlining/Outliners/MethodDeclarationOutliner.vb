' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class MethodDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of MethodStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(methodDeclaration As MethodStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(methodDeclaration, spans)

            Dim methodBlock = TryCast(methodDeclaration.Parent, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                If Not methodBlock.EndBlockStatement.IsMissing Then
                    spans.Add(
                        VisualBasicOutliningHelpers.CreateRegionFromBlock(
                            methodBlock,
                            methodDeclaration.ConvertToSingleLine().ToString() & " " & Ellipsis,
                            autoCollapse:=True))
                End If
            End If
        End Sub
    End Class
End Namespace
