' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class TypeDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of TypeStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(typeDeclaration As TypeStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(typeDeclaration, spans)

            Dim block = TryCast(typeDeclaration.Parent, TypeBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=typeDeclaration, autoCollapse:=False,
                    type:=BlockTypes.Type, isCollapsible:=True))

                CollectCommentsRegions(block.EndBlockStatement, spans)
            End If
        End Sub
    End Class
End Namespace
