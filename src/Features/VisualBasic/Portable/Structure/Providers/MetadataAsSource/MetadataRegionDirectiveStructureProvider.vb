' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
    Friend Class MetadataRegionDirectiveStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of RegionDirectiveTriviaSyntax)

        Private Shared Function GetBannerText(regionDirective As RegionDirectiveTriviaSyntax) As String
            Dim text = regionDirective.Name.ToString().Trim(""""c)

            If text.Length = 0 Then
                Return regionDirective.HashToken.ToString() & regionDirective.RegionKeyword.ToString()
            End If

            Return text
        End Function

        Protected Overrides Sub CollectBlockSpans(regionDirective As RegionDirectiveTriviaSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            Dim match = regionDirective.GetMatchingStartOrEndDirective(cancellationToken)
            If match IsNot Nothing Then
                spans.Add(CreateRegion(
                    TextSpan.FromBounds(regionDirective.SpanStart, match.Span.End),
                    GetBannerText(regionDirective),
                    autoCollapse:=True,
                    type:=BlockTypes.Nonstructural, isCollapsible:=True))
            End If
        End Sub

        Protected Overrides Function SupportedInWorkspaceKind(kind As String) As Boolean
            Return kind = WorkspaceKind.MetadataAsSource
        End Function
    End Class
End Namespace