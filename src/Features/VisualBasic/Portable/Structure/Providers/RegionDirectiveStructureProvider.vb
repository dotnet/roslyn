' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class RegionDirectiveStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of RegionDirectiveTriviaSyntax)

        Private Shared Function GetBannerText(regionDirective As RegionDirectiveTriviaSyntax) As String
            Dim text = regionDirective.Name.ToString().Trim(""""c)

            If text.Length = 0 Then
                Return regionDirective.HashToken.ToString() & regionDirective.RegionKeyword.ToString()
            End If

            Return text
        End Function

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  regionDirective As RegionDirectiveTriviaSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  CancellationToken As CancellationToken)
            Dim matchingDirective = regionDirective.GetMatchingStartOrEndDirective(CancellationToken)
            If matchingDirective IsNot Nothing Then
                ' Always auto-collapse regions for Metadata As Source. These generated files only have one region at the
                ' top of the file, which has content like the following:
                '
                '   #Region "Assembly System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
                '   ' C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll
                '   #End Region
                '
                ' For other files, auto-collapse regions based on the user option.
                Dim autoCollapse = options.IsMetadataAsSource OrElse options.CollapseRegionsWhenCollapsingToDefinitions

                Dim span = TextSpan.FromBounds(regionDirective.SpanStart, matchingDirective.Span.End)
                spans.AddIfNotNull(CreateBlockSpan(
                    span, span,
                    GetBannerText(regionDirective),
                    autoCollapse:=autoCollapse,
                    isDefaultCollapsed:=options.CollapseRegionsWhenFirstOpened,
                    type:=BlockTypes.PreprocessorRegion,
                    isCollapsible:=True))
            End If
        End Sub
    End Class
End Namespace
