' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class ConstructorDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of SubNewStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(constructorDeclaration As SubNewStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            Dim regions As New List(Of OutliningSpan)

            CollectCommentsRegions(constructorDeclaration, spans)

            Dim block = TryCast(constructorDeclaration.Parent, ConstructorBlockSyntax)
            If Not block?.EndBlockStatement.IsMissing Then
                spans.Add(
                    CreateRegionFromBlock(block, bannerNode:=constructorDeclaration, autoCollapse:=True))
            End If
        End Sub
    End Class
End Namespace
