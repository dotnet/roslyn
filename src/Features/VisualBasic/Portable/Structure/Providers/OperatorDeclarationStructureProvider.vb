' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class OperatorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of OperatorStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(operatorDeclaration As OperatorStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(operatorDeclaration, spans)

            Dim block = TryCast(operatorDeclaration.Parent, OperatorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(CreateRegionFromBlock(
                    block, bannerNode:=operatorDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Operator, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace