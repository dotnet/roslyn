' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class RegionDirectiveOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of RegionDirectiveTriviaSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New RegionDirectiveOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub BrokenRegion()
            Const code = "
$$#Region ""Foo""
"

            NoRegions(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub SimpleRegion()
            Const code = "
{|span:$$#Region ""Foo""
#End Region|}
"

            Regions(code,
                Region("span", "Foo", autoCollapse:=False, isDefaultCollapsed:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub RegionWithNoBanner1()
            Const code = "
{|span:$$#Region
#End Region|}
"

            Regions(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub RegionWithNoBanner2()
            Const code = "
{|span:$$#Region """"
#End Region|}
"

            Regions(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Sub

        <WorkItem(537984)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub RegionEndOfFile()
            Const code = "
Class C
End Class
{|span:$$#Region
#End Region|}"

            Regions(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Sub
    End Class
End Namespace
