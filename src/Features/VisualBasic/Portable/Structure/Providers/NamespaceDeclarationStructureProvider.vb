' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class NamespaceDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of NamespaceStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(namespaceDeclaration As NamespaceStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(namespaceDeclaration, spans)

            Dim block = TryCast(namespaceDeclaration.Parent, NamespaceBlockSyntax)
            If Not block?.EndNamespaceStatement.IsMissing Then
                spans.Add(CreateRegionFromBlock(
                    block, bannerNode:=namespaceDeclaration, autoCollapse:=False,
                    type:=BlockTypes.Namespace, isCollapsible:=True))

                CollectCommentsRegions(block.EndNamespaceStatement, spans)
            End If
        End Sub

        Protected Overrides Function SupportedInWorkspaceKind(kind As String) As Boolean
            Return True
        End Function
    End Class
End Namespace