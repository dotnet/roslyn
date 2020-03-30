﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class OperatorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of OperatorStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(operatorDeclaration As OperatorStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  isMetadataAsSource As Boolean,
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(operatorDeclaration, spans, isMetadataAsSource)

            Dim block = TryCast(operatorDeclaration.Parent, OperatorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=operatorDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
