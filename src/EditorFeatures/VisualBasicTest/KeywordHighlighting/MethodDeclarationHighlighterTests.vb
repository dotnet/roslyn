' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class MethodDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New MethodDeclarationHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample1_1() As Task
            Await TestAsync(<Text>
Public Class C1
    WithEvents x As Raiser
    {|Cursor:[|Sub|]|} E1Handler() [|Handles|] x.E1
        'Do Nothing
    [|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample1_2() As Task
            Await TestAsync(<Text>
Public Class C1
    WithEvents x As Raiser
    [|Sub|] E1Handler() {|Cursor:[|Handles|]|} x.E1
        'Do Nothing
    [|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample1_3() As Task
            Await TestAsync(<Text>
Public Class C1
    WithEvents x As Raiser
    [|Sub|] E1Handler() [|Handles|] x.E1
        'Do Nothing
    {|Cursor:[|End Sub|]|}
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample2_1() As Task
            Await TestAsync(<Text>
Public Class C1
    {|Cursor:[|Public Shared Sub|]|} Foo()
        [|Exit Sub|]
    [|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample2_2() As Task
            Await TestAsync(<Text>
Public Class C1
    [|Public Shared Sub|] Foo()
        {|Cursor:[|Exit Sub|]|}
    [|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample2_3() As Task
            Await TestAsync(<Text>
Public Class C1
    [|Public Shared Sub|] Foo()
        [|Exit Sub|]
    {|Cursor:[|End Sub|]|}
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample3_1() As Task
            Await TestAsync(<Text>
Public Class C1
    Implements IDisposable
    {|Cursor:[|Public Sub|]|} Dispose() [|Implements|] IDisposable.Dispose
        'Do Nothing
    [|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample3_2() As Task
            Await TestAsync(<Text>
Public Class C1
    Implements IDisposable
    [|Public Sub|] Dispose() {|Cursor:[|Implements|]|} IDisposable.Dispose
        'Do Nothing
    [|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample3_3() As Task
            Await TestAsync(<Text>
Public Class C1
    Implements IDisposable
    [|Public Sub|] Dispose() [|Implements|] IDisposable.Dispose
        'Do Nothing
    {|Cursor:[|End Sub|]|}
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample4_1() As Task
            Await TestAsync(<Text>
Public Class C1
    {|Cursor:[|Public Overrides Function|]|} ToString() As String
        [|Return|] Nothing
    [|End Function|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample4_2() As Task
            Await TestAsync(<Text>
Public Class C1
    [|Public Overrides Function|] ToString() As String
        {|Cursor:[|Return|]|} Nothing
    [|End Function|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethodExample4_3() As Task
            Await TestAsync(<Text>
Public Class C1
    [|Public Overrides Function|] ToString() As String
        [|Return|] Nothing
    {|Cursor:[|End Function|]|}
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_AsyncExample1_1() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    {|Cursor:[|Async Sub|]|} UseAsync()
        Dim lambda = Async Function()
                         Return Await AsyncMethod()
                     End Function

        Dim result = [|Await|] AsyncMethod()

        [|Exit Sub|]

        Dim resultTask = AsyncMethod()
        result = [|Await|] resultTask

        result = [|Await|] lambda()
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_AsyncExample1_2() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    [|Async Sub|] UseAsync()
        Dim lambda = Async Function()
                         Return Await AsyncMethod()
                     End Function

        Dim result = {|Cursor:[|Await|]|} AsyncMethod()

        [|Exit Sub|]

        Dim resultTask = AsyncMethod()
        result = [|Await|] resultTask

        result = [|Await|] lambda()
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_AsyncExample1_3() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    [|Async Sub|] UseAsync()
        Dim lambda = Async Function()
                         Return Await AsyncMethod()
                     End Function

        Dim result = [|Await|] AsyncMethod()

        {|Cursor:[|Exit Sub|]|}

        Dim resultTask = AsyncMethod()
        result = [|Await|] resultTask

        result = [|Await|] lambda()
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_AsyncExample1_4() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    [|Async Sub|] UseAsync()
        Dim lambda = Async Function()
                         Return Await AsyncMethod()
                     End Function

        Dim result = [|Await|] AsyncMethod()

        [|Exit Sub|]

        Dim resultTask = AsyncMethod()
        result = {|Cursor:[|Await|]|} resultTask

        result = [|Await|] lambda()
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_AsyncExample1_5() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    [|Async Sub|] UseAsync()
        Dim lambda = Async Function()
                         Return Await AsyncMethod()
                     End Function

        Dim result = [|Await|] AsyncMethod()

        [|Exit Sub|]

        Dim resultTask = AsyncMethod()
        result = [|Await|] resultTask

        result = {|Cursor:[|Await|]|} lambda()
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_AsyncExample1_6() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Integer)
        Dim hours = 24
        Return hours
    End Function

    [|Async Sub|] UseAsync()
        Dim lambda = Async Function()
                         Return Await AsyncMethod()
                     End Function

        Dim result = [|Await|] AsyncMethod()

        [|Exit Sub|]

        Dim resultTask = AsyncMethod()
        result = [|Await|] resultTask

        result = [|Await|] lambda()
    {|Cursor:[|End Sub|]|}
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_Async_NestedAwaits1() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    {|Cursor:[|Async Sub|]|} Foo()
        Dim t = Task.FromResult(Task.FromResult(1))
        Dim value = [|Await Await|] t
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_Async_NestedAwaits2() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    [|Async Sub|] Foo()
        Dim t = Task.FromResult(Task.FromResult(1))
        Dim value = {|Cursor:[|Await Await|]|} t
    [|End Sub|]
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_Async_NestedAwaits3() As Task
            Await TestAsync(
<Text>
Imports System.Threading.Tasks

Class AsyncExample
    [|Async Sub|] Foo()
        Dim t = Task.FromResult(Task.FromResult(1))
        Dim value = [|Await Await|] t
    {|Cursor:[|End Sub|]|}
End Class

</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_IteratorExample1_1() As Task
            Await TestAsync(
<Text>
{|Cursor:[|Iterator Function|]|} Test() As IEnumerable(Of Integer)

    Dim listFunction = Iterator Function()
                           Yield 5
                           Yield 15

                           Exit Function

                           Yield 25
                       End Function

    [|Yield|] 1

    [|Return|]

    For Each i In listFunction()
        [|Yield|] i
    Next
[|End Function|]
</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_IteratorExample1_2() As Task
            Await TestAsync(
<Text>
[|Iterator Function|] Test() As IEnumerable(Of Integer)

    Dim listFunction = Iterator Function()
                           Yield 5
                           Yield 15

                           Exit Function

                           Yield 25
                       End Function

    {|Cursor:[|Yield|]|} 1

    [|Return|]

    For Each i In listFunction()
        [|Yield|] i
    Next
[|End Function|]
</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_IteratorExample1_3() As Task
            Await TestAsync(
<Text>
[|Iterator Function|] Test() As IEnumerable(Of Integer)

    Dim listFunction = Iterator Function()
                           Yield 5
                           Yield 15

                           Exit Function

                           Yield 25
                       End Function

    [|Yield|] 1

    {|Cursor:[|Return|]|}

    For Each i In listFunction()
        [|Yield|] i
    Next
[|End Function|]
</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_IteratorExample1_4() As Task
            Await TestAsync(
<Text>
[|Iterator Function|] Test() As IEnumerable(Of Integer)

    Dim listFunction = Iterator Function()
                           Yield 5
                           Yield 15

                           Exit Function

                           Yield 25
                       End Function

    [|Yield|] 1

    [|Return|]

    For Each i In listFunction()
        {|Cursor:[|Yield|]|} i
    Next
[|End Function|]
</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMethod_IteratorExample1_5() As Task
            Await TestAsync(
<Text>
[|Iterator Function|] Test() As IEnumerable(Of Integer)

    Dim listFunction = Iterator Function()
                           Yield 5
                           Yield 15

                           Exit Function

                           Yield 25
                       End Function

    [|Yield|] 1

    [|Return|]

    For Each i In listFunction()
        [|Yield|] i
    Next
{|Cursor:[|End Function|]|}
</Text>)
        End Function

    End Class
End Namespace
