' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class NamespaceDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of NamespaceStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  namespaceDeclaration As NamespaceStatementSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(namespaceDeclaration, spans, options)

            Dim block = TryCast(namespaceDeclaration.Parent, NamespaceBlockSyntax)
            If Not block?.EndNamespaceStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=namespaceDeclaration, autoCollapse:=False,
                    type:=BlockTypes.Namespace, isCollapsible:=True))

                CollectCommentsRegions(block.EndNamespaceStatement, spans, options)
            End If
        End Sub
    End Class
End Namespace
