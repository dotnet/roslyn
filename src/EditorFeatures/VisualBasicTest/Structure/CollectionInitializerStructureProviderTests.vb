' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class CollectionInitializerStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of CollectionInitializerSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New CollectionInitializerStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestInnerInitializer() As Task
            Const code = "
Class C
    Sub M()
        dim d = new Dictionary(of integer, string) From {
            {|hintspan:{|textspan:$${
                1, ""goo""
            },|}|}
            {
                1, ""goo""
            }
        }
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hintspan", bannerText:="...", autoCollapse:=False))
        End Function
    End Class
End Namespace
