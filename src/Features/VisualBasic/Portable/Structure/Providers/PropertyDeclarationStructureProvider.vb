' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class PropertyDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of PropertyStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  propertyDeclaration As PropertyStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(propertyDeclaration, spans, options)

            Dim block = TryCast(propertyDeclaration.Parent, PropertyBlockSyntax)
            If Not block?.EndPropertyStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=propertyDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))

                CollectCommentsRegions(block.EndPropertyStatement, spans, options)
            End If
        End Sub
    End Class
End Namespace
