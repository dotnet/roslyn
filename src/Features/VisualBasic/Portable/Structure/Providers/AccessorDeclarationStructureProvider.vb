' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class AccessorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of AccessorStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(accessorDeclaration As AccessorStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(accessorDeclaration, spans)

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
