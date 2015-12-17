' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class RegionDirectiveOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of RegionDirectiveTriviaSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New RegionDirectiveOutliner()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function BrokenRegion() As Task
            Const code = "
$$#Region ""Foo""
"

            Await VerifyNoRegionsAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function SimpleRegion() As Task
            Const code = "
{|span:$$#Region ""Foo""
#End Region|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "Foo", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function RegionWithNoBanner1() As Task
            Const code = "
{|span:$$#Region
#End Region|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function RegionWithNoBanner2() As Task
            Const code = "
{|span:$$#Region """"
#End Region|}
"

            Await VerifyRegionsAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <WorkItem(537984)>
        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function RegionEndOfFile() As Task
            Const code = "
Class C
End Class
{|span:$$#Region
#End Region|}"

            Await VerifyRegionsAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function
    End Class
End Namespace
