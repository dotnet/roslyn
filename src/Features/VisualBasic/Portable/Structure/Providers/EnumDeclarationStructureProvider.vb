' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class EnumDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of EnumStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  enumDeclaration As EnumStatementSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(enumDeclaration, spans, options)

            Dim block = TryCast(enumDeclaration.Parent, EnumBlockSyntax)
            If Not block?.EndEnumStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=enumDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Type, isCollapsible:=True))

                CollectCommentsRegions(block.EndEnumStatement, spans, options)
            End If
        End Sub
    End Class
End Namespace
