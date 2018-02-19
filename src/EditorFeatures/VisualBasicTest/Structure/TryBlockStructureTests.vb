' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class TryBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of TryBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New TryBlockStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:Try $$
        Catch (e As Exception)
        End Try|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Try ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock2() As Task
            Const code = "
Class C
    Sub M()
        {|span:Try $$
        Finally
        End Try|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Try ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
