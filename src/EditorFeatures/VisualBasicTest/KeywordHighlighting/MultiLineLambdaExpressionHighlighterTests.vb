' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class MultiLineLambdaExpressionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(MultiLineLambdaExpressionHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = {|Cursor:[|Function|]|}(x As Integer)
            If x = 0 Then [|Return|] -1 Else [|Exit Function|]
        [|End Function|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = [|Function|](x As Integer)
            If x = 0 Then {|Cursor:[|Return|]|} -1 Else [|Exit Function|]
        [|End Function|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = [|Function|](x As Integer)
            If x = 0 Then [|Return|] -1 Else {|Cursor:[|Exit Function|]|}
        [|End Function|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = [|Function|](x As Integer)
            If x = 0 Then [|Return|] -1 Else [|Exit Function|]
        {|Cursor:[|End Function|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineSubLambda1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = {|Cursor:[|Sub|]|}(x As Integer)
            If x = 0 Then [|Return|] Else [|Exit Sub|]
        [|End Sub|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineSubLambda2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = [|Sub|](x As Integer)
            If x = 0 Then {|Cursor:[|Return|]|} Else [|Exit Sub|]
        [|End Sub|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineSubLambda3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = [|Sub|](x As Integer)
            If x = 0 Then [|Return|] Else {|Cursor:[|Exit Sub|]|}
        [|End Sub|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineSubLambda4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
Dim f = [|Sub|](x As Integer)
            If x = 0 Then [|Return|] Else [|Exit Sub|]
        {|Cursor:[|End Sub|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineLambda_AsyncExample2_1() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    Async Sub UseAsync()
        Dim lambda = {|Cursor:[|Async Function|]|}()
                         [|Return Await|] AsyncMethod()
                     [|End Function|]

        Dim result = Await AsyncMethod()

        Exit Sub

        Dim resultTask = AsyncMethod()
        result = Await resultTask

        result = Await lambda()
    End Sub
End Class

</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineLambda_AsyncExample2_2() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    Async Sub UseAsync()
        Dim lambda = [|Async Function|]()
                         {|Cursor:[|Return Await|]|} AsyncMethod()
                     [|End Function|]

        Dim result = Await AsyncMethod()

        Exit Sub

        Dim resultTask = AsyncMethod()
        result = Await resultTask

        result = Await lambda()
    End Sub
End Class

</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineLambda_AsyncExample2_3() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    Async Sub UseAsync()
        Dim lambda = [|Async Function|]()
                         [|Return Await|] AsyncMethod()
                     {|Cursor:[|End Function|]|}

        Dim result = Await AsyncMethod()

        Exit Sub

        Dim resultTask = AsyncMethod()
        result = Await resultTask

        result = Await lambda()
    End Sub
End Class

</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda_IteratorExample2_1() As Task
            Await TestAsync(
<Text>
Iterator Function Test() As IEnumerable(Of Integer)

    Dim listFunction = {|Cursor:[|Iterator Function|]|}()
                           [|Yield|] 5
                           [|Yield|] 15

                           [|Exit Function|]

                           [|Yield|] 25
                       [|End Function|]

    Yield 1

    Return

    For Each i In listFunction()
        Yield i
    Next
End Function
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda_IteratorExample2_2() As Task
            Await TestAsync(
<Text>
Iterator Function Test() As IEnumerable(Of Integer)

    Dim listFunction = [|Iterator Function|]()
                           {|Cursor:[|Yield|]|} 5
                           [|Yield|] 15

                           [|Exit Function|]

                           [|Yield|] 25
                       [|End Function|]

    Yield 1

    Return

    For Each i In listFunction()
        Yield i
    Next
End Function
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda_IteratorExample2_3() As Task
            Await TestAsync(
<Text>
Iterator Function Test() As IEnumerable(Of Integer)

    Dim listFunction = [|Iterator Function|]()
                           [|Yield|] 5
                           {|Cursor:[|Yield|]|} 15

                           [|Exit Function|]

                           [|Yield|] 25
                       [|End Function|]

    Yield 1

    Return

    For Each i In listFunction()
        Yield i
    Next
End Function
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda_IteratorExample2_4() As Task
            Await TestAsync(
<Text>
Iterator Function Test() As IEnumerable(Of Integer)

    Dim listFunction = [|Iterator Function|]()
                           [|Yield|] 5
                           [|Yield|] 15

                           {|Cursor:[|Exit Function|]|}

                           [|Yield|] 25
                       [|End Function|]

    Yield 1

    Return

    For Each i In listFunction()
        Yield i
    Next
End Function
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda_IteratorExample2_5() As Task
            Await TestAsync(
<Text>
Iterator Function Test() As IEnumerable(Of Integer)

    Dim listFunction = [|Iterator Function|]()
                           [|Yield|] 5
                           [|Yield|] 15

                           [|Exit Function|]

                           {|Cursor:[|Yield|]|} 25
                       [|End Function|]

    Yield 1

    Return

    For Each i In listFunction()
        Yield i
    Next
End Function
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineFunctionLambda_IteratorExample2_6() As Task
            Await TestAsync(
<Text>
Iterator Function Test() As IEnumerable(Of Integer)

    Dim listFunction = [|Iterator Function|]()
                           [|Yield|] 5
                           [|Yield|] 15

                           [|Exit Function|]

                           [|Yield|] 25
                       {|Cursor:[|End Function|]|}

    Yield 1

    Return

    For Each i In listFunction()
        Yield i
    Next
End Function
</Text>)
        End Function

    End Class
End Namespace
