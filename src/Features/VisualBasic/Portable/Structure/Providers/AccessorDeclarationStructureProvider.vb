﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class AccessorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of AccessorStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(accessorDeclaration As AccessorStatementSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  optionProvider As BlockStructureOptionProvider,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(accessorDeclaration, spans, optionProvider)

            Dim block = TryCast(accessorDeclaration.Parent, AccessorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=accessorDeclaration,
                    autoCollapse:=True, type:=BlockTypes.Member,
                    isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
