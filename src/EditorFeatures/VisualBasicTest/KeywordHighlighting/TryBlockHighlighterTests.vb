' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class TryBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New TryBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestTryBlock1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestTryBlock2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestTryBlock3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestTryBlock4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestTryBlock5()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestTryBlock6()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestExitTryInCatchBlock()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestExitTryInCatchBlock2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub NegativeTestExitTryInNestedTry()
            Test(<Text>
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
        End Sub
    End Class
End Namespace
