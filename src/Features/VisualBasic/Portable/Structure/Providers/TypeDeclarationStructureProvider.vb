﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class TypeDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of TypeStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(typeDeclaration As TypeStatementSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  optionProvider As BlockStructureOptionProvider,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(typeDeclaration, spans, optionProvider)

            Dim block = TryCast(typeDeclaration.Parent, TypeBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=typeDeclaration, autoCollapse:=False,
                    type:=BlockTypes.Type, isCollapsible:=True))

                CollectCommentsRegions(block.EndBlockStatement, spans, optionProvider)
            End If
        End Sub
    End Class
End Namespace
