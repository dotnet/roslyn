' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class ConstructorDeclarationOutliner
        Inherits AbstractSyntaxNodeOutliner(Of SubNewStatementSyntax)

        Protected Overrides Sub CollectOutliningSpans(constructorDeclaration As SubNewStatementSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            Dim regions As New List(Of OutliningSpan)

            VisualBasicOutliningHelpers.CollectCommentsRegions(constructorDeclaration, spans)

            Dim methodBlock = TryCast(constructorDeclaration.Parent, ConstructorBlockSyntax)
            If methodBlock IsNot Nothing Then
                If Not methodBlock.EndBlockStatement.IsMissing Then
                    spans.Add(
                        VisualBasicOutliningHelpers.CreateRegionFromBlock(
                            methodBlock,
                            constructorDeclaration.ConvertToSingleLine().ToString() & " " & Ellipsis,
                            autoCollapse:=True))
                End If
            End If
        End Sub
    End Class
End Namespace
