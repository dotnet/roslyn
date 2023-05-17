' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class RegionDirectiveStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of RegionDirectiveTriviaSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New RegionDirectiveStructureProvider()
        End Function

        <Fact>
        Public Async Function BrokenRegion() As Task
            Const code = "
$$#Region ""Goo""
"

            Await VerifyNoBlockSpansAsync(code)
        End Function

        <Fact>
        Public Async Function SimpleRegion() As Task
            Const code = "
{|span:$$#Region ""Goo""
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Goo", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Theory, CombinatorialData>
        Public Async Function RegionsShouldBeCollapsedByDefault(collapseRegionsByDefault As Boolean) As Task
            Const code = "
{|span:$$#Region ""Goo""
#End Region|}
"

            Dim options = New BlockStructureOptions() With {.CollapseRegionsWhenFirstOpened = collapseRegionsByDefault}

            Await VerifyBlockSpansAsync(code, options,
                Region("span", "Goo", autoCollapse:=False, isDefaultCollapsed:=collapseRegionsByDefault))
        End Function

        <Fact>
        Public Async Function RegionWithNoBanner1() As Task
            Const code = "
{|span:$$#Region
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Fact>
        Public Async Function RegionWithNoBanner2() As Task
            Const code = "
{|span:$$#Region """"
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537984")>
        Public Async Function RegionEndOfFile() As Task
            Const code = "
Class C
End Class
{|span:$$#Region
#End Region|}"

            Await VerifyBlockSpansAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function
    End Class
End Namespace
