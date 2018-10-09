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
        {|span0:Try $$
        Catch (e As Exception)
        End Try|}
    End Sub
End Class
"

            Dim regions = {
                            Region("span0", "Try ...", autoCollapse:=False)
                          }
            Await VerifyBlockSpansAsync(code, regions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock1a() As Task
            Const code = "
Class C
    Sub M()
        {|span0:Try $$
            ' Comment
        Catch e As Exception
        End Try|}
    End Sub
End Class
"

            Dim regions = {
                            Region("span0", "Try ...", autoCollapse:=False)
                          }
            Await VerifyBlockSpansAsync(code, regions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock1b() As Task
            Const code = "
Class C
    Sub M()
        {|span0:Try $$
{|span1:            ' Comment 0
            Console.WriteLine(""TryBlock"")
            ' Comment 1|}
        Catch e As Exception
        End Try|}
    End Sub
End Class
"

            Dim regions = {
                            Region("span0", "Try ...", autoCollapse:=False),
                            Region("span1", "...", autoCollapse:=False)
                          }
            Await VerifyBlockSpansAsync(code, regions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock2() As Task
            Const code = "
Class C
    Sub M()
        {|span0:Try $$
        Finally
        End Try|}
    End Sub
End Class
"
            Dim regions = {
                            Region("span0", "Try ...", autoCollapse:=False)
                          }
            Await VerifyBlockSpansAsync(code, regions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock2a() As Task
            Const code = "
Class C
    Sub M()
        {|span0:Try $$
        {|span1:Finally
            'Comment 0
            Console.WriteLine(""Finally Block"")
            'Comment 1|}
        End Try|}
    End Sub
End Class
"
            Dim regions = {
                            Region("span0", "Try ...", autoCollapse:=False),
                            Region("span1", "Finally", autoCollapse:=False)
                          }
            Await VerifyBlockSpansAsync(code, regions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestTryBlock3() As Task
            Const code = "
Class C
    Sub M()
        {|span0:Try $$
{|span1:            ' 
            Console.WriteLine()
            '|}
        {|span2:Catch e As Exception
            ' 
            Console.WriteLine()
            '|}
        {|span3:Finally
            ' 
            Console.WriteLine()
            '|}
        End Try|}
    End Sub
End Class
"
            Dim regions = {
                            Region("span0", "Try ...", autoCollapse:=False),
                            Region("span1", "...", autoCollapse:=False),
                            Region("span2", "Catch e As Exception", autoCollapse:=False),
                            Region("span3", "Finally", autoCollapse:=False)
                          }
            Await VerifyBlockSpansAsync(code, regions)
        End Function

    End Class
End Namespace
