' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend MustInherit Class AbstractSyntaxNodeOutliner(Of TSyntaxNode As SyntaxNode)
        Inherits AbstractSyntaxNodeOutliner

        Public Overrides Sub CollectOutliningSpans(document As Document, node As SyntaxNode, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            If Not SupportedInWorkspaceKind(document.Project.Solution.Workspace.Kind) Then
                Return
            End If

            CollectOutliningSpans(node, spans, cancellationToken)
        End Sub

        Friend Overloads Sub CollectOutliningSpans(node As SyntaxNode, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            If TypeOf node Is TSyntaxNode Then
                CollectOutliningSpans(DirectCast(node, TSyntaxNode), spans, cancellationToken)
            End If
        End Sub

        ' For testing purposes
        Friend Overloads Function GetOutliningSpans(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of OutliningSpan)
            Dim spans = New List(Of OutliningSpan)
            CollectOutliningSpans(node, spans, cancellationToken)
            Return spans
        End Function

        Protected Overridable Function SupportedInWorkspaceKind(kind As String) As Boolean
            ' We have other outliners specific to Metadata-as-Source
            Return kind <> WorkspaceKind.MetadataAsSource
        End Function


        Protected MustOverride Overloads Sub CollectOutliningSpans(node As TSyntaxNode, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
    End Class
End Namespace
