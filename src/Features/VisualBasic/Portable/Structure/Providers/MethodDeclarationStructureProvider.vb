' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class MethodDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of MethodStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(methodDeclaration As MethodStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(methodDeclaration, spans)

            Dim block = TryCast(methodDeclaration.Parent, MethodBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.AddIfNotNull(CreateBlockSpanFromBlock(
                    block, bannerNode:=methodDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Member, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
