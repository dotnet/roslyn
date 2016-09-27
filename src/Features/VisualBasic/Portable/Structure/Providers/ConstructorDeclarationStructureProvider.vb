' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class ConstructorDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of SubNewStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(constructorDeclaration As SubNewStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            Dim regions As New List(Of BlockSpan)

            CollectCommentsRegions(constructorDeclaration, spans)

            Dim block = TryCast(constructorDeclaration.Parent, ConstructorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(CreateRegionFromBlock(
                    block, bannerNode:=constructorDeclaration, autoCollapse:=True,
                    type:=BlockTypes.Constructor, isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
