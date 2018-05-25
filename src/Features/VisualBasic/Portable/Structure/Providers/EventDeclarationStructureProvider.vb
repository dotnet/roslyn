' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(eventDeclaration, spans)

            Dim block = TryCast(eventDeclaration.Parent, EventBlockSyntax)
            If Not block?.EndEventStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=eventDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))

                CollectCommentsRegions(block.EndEventStatement, spans)
            End If
        End Sub
    End Class
End Namespace
