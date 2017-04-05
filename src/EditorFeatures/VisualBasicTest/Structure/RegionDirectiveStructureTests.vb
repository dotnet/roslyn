' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class RegionDirectiveStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of RegionDirectiveTriviaSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New RegionDirectiveStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function BrokenRegion() As Task
            Const code = "
$$#Region ""Foo""
"

            Await VerifyNoBlockSpansAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function SimpleRegion() As Task
            Const code = "
{|span:$$#Region ""Foo""
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Foo", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function RegionWithNoBanner1() As Task
            Const code = "
{|span:$$#Region
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function RegionWithNoBanner2() As Task
            Const code = "
{|span:$$#Region """"
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "#Region", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

        <WorkItem(537984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537984")>
        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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