' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class NamespaceDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of NamespaceStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(namespaceDeclaration As NamespaceStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            VisualBasicOutliningHelpers.CollectCommentsRegions(namespaceDeclaration, spans)

            Dim namespaceBlock = TryCast(namespaceDeclaration.Parent, NamespaceBlockSyntax)
            If Not namespaceBlock.EndNamespaceStatement.IsMissing Then
                spans.Add(
                    VisualBasicOutliningHelpers.CreateRegionFromBlock(
                        namespaceBlock,
                        namespaceDeclaration.ConvertToSingleLine().ToString() & " " & Ellipsis,
                        autoCollapse:=False))

                VisualBasicOutliningHelpers.CollectCommentsRegions(namespaceBlock.EndNamespaceStatement, spans)
            End If
        End Sub

        Protected Overrides Function SupportedInWorkspaceKind(kind As String) As Boolean
            Return True
        End Function
    End Class
End Namespace
