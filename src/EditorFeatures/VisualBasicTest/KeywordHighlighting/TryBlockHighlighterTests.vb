' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class TryBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New TryBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestTryBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Try|]|}
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception [|When|] Foo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestTryBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    {|Cursor:[|Exit Try|]|}
[|Catch|] e As Exception [|When|] Foo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestTryBlock3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
{|Cursor:[|Catch|]|} e As Exception [|When|] Foo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestTryBlock4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception {|Cursor:[|When|]|} Foo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestTryBlock5() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception [|When|] Foo()
    Console.WriteLine("Caught exception!")
{|Cursor:[|Finally|]|}
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestTryBlock6() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
    [|Exit Try|]
[|Catch|] e As Exception [|When|] Foo()
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
{|Cursor:[|End Try|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestExitTryInCatchBlock() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
[|Catch|] e As Exception [|When|] Foo()
    [|Exit Try|]
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
{|Cursor:[|End Try|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestExitTryInCatchBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Try|]
    Throw New AppDomainUnloadedException
[|Catch|] e As Exception [|When|] Foo()
    {|Cursor:[|Exit Try|]|}
    Console.WriteLine("Caught exception!")
[|Finally|]
    Console.WriteLine("Exiting try.")
[|End Try|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
