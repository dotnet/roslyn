﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class EventDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of EventStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(eventDeclaration As EventStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  isMetadataAsSource As Boolean,
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(eventDeclaration, spans, isMetadataAsSource)

            Dim block = TryCast(eventDeclaration.Parent, EventBlockSyntax)
            If Not block?.EndEventStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=eventDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))

                CollectCommentsRegions(block.EndEventStatement, spans, isMetadataAsSource)
            End If
        End Sub
    End Class
End Namespace
