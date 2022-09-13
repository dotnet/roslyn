' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class TryBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(TryBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestTryBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Try|]|}
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception [|When|] Goo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestTryBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    {|Cursor:[|Exit Try|]|}
[|Catch|] e As Exception [|When|] Goo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestTryBlock3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
{|Cursor:[|Catch|]|} e As Exception [|When|] Goo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestTryBlock4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception {|Cursor:[|When|]|} Goo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestTryBlock5() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception [|When|] Goo()
    Console.WriteLine("Caught exception!")
{|Cursor:[|Finally|]|}
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestTryBlock6() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception [|When|] Goo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
{|Cursor:[|End Try|]|}
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestExitTryInCatchBlock() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
[|Catch|] e As Exception [|When|] Goo()
    [|Exit Try|]
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
{|Cursor:[|End Try|]|}
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestExitTryInCatchBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
[|Catch|] e As Exception [|When|] Goo()
    {|Cursor:[|Exit Try|]|}
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function NegativeTestExitTryInNestedTry() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Try|]|}
[|Catch|] e As Exception
    Try
    Catch e As Exception
        Exit Try
    End Try
[|End Try|]
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
