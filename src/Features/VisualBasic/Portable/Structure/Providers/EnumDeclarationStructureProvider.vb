' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class EnumDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of EnumStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(enumDeclaration As EnumStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(enumDeclaration, spans)

            Dim block = TryCast(enumDeclaration.Parent, EnumBlockSyntax)
            If Not block?.EndEnumStatement.IsMissing Then
                spans.Add(CreateRegionFromBlock(
                    block, bannerNode:=enumDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Enum, isCollapsible:=True))

                CollectCommentsRegions(block.EndEnumStatement, spans)
            End If
        End Sub
    End Class
End Namespace