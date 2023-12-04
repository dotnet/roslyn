' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class ObjectCreationInitializerStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of ObjectCreationInitializerSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New ObjectCreationInitializerStructureProvider()
        End Function

        <Fact>
        Public Async Function TestCollectionInitializer() As Task
            Const code = "
Class C
    Sub M()
        dim d = {|hintspan:new Dictionary(of integer, string){|textspan: $$From {
            { 1, ""goo"" }
        }|}|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hintspan", bannerText:="...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestMemberInitializer() As Task
            Const code = "
Class C
    private i as integer
    Sub M()
        dim d = {|hintspan:new C{|textspan: $$With {
            .i = 1
        }|}|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hintspan", bannerText:="...", autoCollapse:=False))
        End Function
    End Class
End Namespace
