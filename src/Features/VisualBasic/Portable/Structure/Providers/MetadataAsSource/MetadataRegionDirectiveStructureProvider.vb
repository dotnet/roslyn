' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
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
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            Dim match = regionDirective.GetMatchingStartOrEndDirective(cancellationToken)
            If match IsNot Nothing Then
                Dim span = TextSpan.FromBounds(regionDirective.SpanStart, match.Span.End)
                spans.AddIfNotNull(CreateBlockSpan(
                    span, span,
                    GetBannerText(regionDirective),
                    autoCollapse:=True, type:=BlockTypes.PreprocessorRegion,
                    isCollapsible:=True, isDefaultCollapsed:=False))
            End If
        End Sub

        Protected Overrides Function SupportedInWorkspaceKind(kind As String) As Boolean
            Return kind = WorkspaceKind.MetadataAsSource
        End Function
    End Class
End Namespace
