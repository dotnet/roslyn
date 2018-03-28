' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class PropertyDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of PropertyStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(propertyDeclaration As PropertyStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(propertyDeclaration, spans)

            Dim block = TryCast(propertyDeclaration.Parent, PropertyBlockSyntax)
            If Not block?.EndPropertyStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=propertyDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))

                CollectCommentsRegions(block.EndPropertyStatement, spans)
            End If
        End Sub
    End Class
End Namespace
