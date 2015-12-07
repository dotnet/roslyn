' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class OperatorDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of OperatorStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(operatorDeclaration As OperatorStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            Dim methodBlock = TryCast(operatorDeclaration.Parent, OperatorBlockSyntax)
            If methodBlock IsNot Nothing Then
                VisualBasicOutliningHelpers.CollectCommentsRegions(methodBlock, spans)

                If Not methodBlock.EndBlockStatement.IsMissing Then
                    spans.Add(
                        VisualBasicOutliningHelpers.CreateRegionFromBlock(
                            methodBlock,
                            operatorDeclaration.ConvertToSingleLine().ToString() & " " & Ellipsis,
                            autoCollapse:=True))
                End If
            End If
        End Sub
    End Class
End Namespace
