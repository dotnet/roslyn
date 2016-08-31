' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class TypeDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of TypeStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(typeDeclaration As TypeStatementSyntax,
                                                  spans As ImmutableArray(Of BlockSpan).Builder,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(typeDeclaration, spans)

            Dim block = TryCast(typeDeclaration.Parent, TypeBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=typeDeclaration, autoCollapse:=False))

                CollectCommentsRegions(block.EndBlockStatement, spans)
            End If
        End Sub
    End Class
End Namespace