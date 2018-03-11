' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class ObjectCreationInitializerStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of ObjectCreationInitializerSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New ObjectCreationInitializerStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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
