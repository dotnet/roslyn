' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.ExtractMethod
    <Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)>
    Public Class ExtractMethodTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New ExtractMethodCodeRefactoringProvider()
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540686")>
        Public Async Function TestExtractReturnExpression() As Task
            Await TestInRegularAndScriptAsync(
"Class Module1
    Private Delegate Function Func(i As Integer)
    Shared Sub Main(args As String())
        Dim temp As Integer = 2
        Dim fnc As Func = Function(arg As Integer)
                              temp = arg
                              Return [|arg|]
                          End Function
    End Sub
End Class",
"Class Module1
    Private Delegate Function Func(i As Integer)
    Shared Sub Main(args As String())
        Dim temp As Integer = 2
        Dim fnc As Func = Function(arg As Integer)
                              temp = arg
                              Return {|Rename:GetArg|}(arg)
                          End Function
    End Sub

    Private Shared Function GetArg(arg As Integer) As Integer
        Return arg
    End Function
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540755")>
        Public Async Function TestExtractMultilineLambda() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Dim q As Action = [|Sub()
                                       End Sub|]
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        If True Then Dim q As Action = {|Rename:GetQ|}()
    End Sub

    Private Function GetQ() As Action
        Return Sub()
               End Sub
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541515")>
        Public Async Function TestCollectionInitializerInObjectCollectionInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main()
        [|Dim x As New List(Of Program) From {New Program}|]
    End Sub
    Public Property Name As String
End Class",
"Class Program
    Sub Main()
        {|Rename:NewMethod|}()
    End Sub

    Private Shared Sub NewMethod()
        Dim x As New List(Of Program) From {New Program}
    End Sub

    Public Property Name As String
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542251")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543030")>
        Public Async Function TestLambdaSelection() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim q As Object
        If True Then q = [|Sub()
                         End Sub|]
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim q As Object
        If True Then q = {|Rename:NewMethod|}()
    End Sub

    Private Function NewMethod() As Object
        Return Sub()
               End Sub
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542904")>
        Public Async Function TestFormatBeforeAttribute() As Task
            Await TestInRegularAndScriptAsync(
<Text>Module Program
    Sub Main(args As String())
        Dim x = [|1 + 1|]
    End Sub

    &lt;Obsolete&gt;
    Sub Goo
    End Sub
End Module
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Module Program
    Sub Main(args As String())
        Dim x = {|Rename:GetX|}()
    End Sub

    Private Function GetX() As Integer
        Return 1 + 1
    End Function

    &lt;Obsolete&gt;
    Sub Goo
    End Sub
End Module
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545262")>
        Public Async Function TestInTernaryConditional() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim p As Object = Nothing
        Dim Obj1 = If(New With {.a = True}.a, p, [|Nothing|])
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim p As Object = Nothing
        Dim Obj1 = If(New With {.a = True}.a, p, {|Rename:NewMethod|}())
    End Sub

    Private Function NewMethod() As Object
        Return Nothing
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545547")>
        Public Async Function TestInRangeArgumentUpperBound() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        Dim x(0 To [|1 + 2|]) ' Extract method 
    End Sub
End Module",
"Module Program
    Sub Main()
        Dim x(0 To {|Rename:NewMethod|}()) ' Extract method 
    End Sub

    Private Function NewMethod() As Integer
        Return 1 + 2
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545655")>
        Public Async Function TestInWhileUntilCondition() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Sub Main()
        Dim x = 0
        Do While [|x * x < 100|]
            x += 1
        Loop
    End Sub
End Module",
"Module M
    Sub Main()
        Dim x = 0
        Do While {|Rename:NewMethod|}(x)
            x += 1
        Loop
    End Sub

    Private Function NewMethod(x As Integer) As Boolean
        Return x * x < 100
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestInInterpolation1() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Sub Main()
        Dim v As New Object
        [|System.Console.WriteLine($""{v}"")|]
        System.Console.WriteLine(v)
    End Sub
End Module",
"Module M
    Sub Main()
        Dim v As New Object
        {|Rename:NewMethod|}(v)
        System.Console.WriteLine(v)
    End Sub

    Private Sub NewMethod(v As Object)
        System.Console.WriteLine($""{v}"")
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestInInterpolation2() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Sub Main()
        Dim v As New Object
        System.Console.WriteLine([|$""{v}""|])
        System.Console.WriteLine(v)
    End Sub
End Module",
"Module M
    Sub Main()
        Dim v As New Object
        System.Console.WriteLine({|Rename:NewMethod|}(v))
        System.Console.WriteLine(v)
    End Sub

    Private Function NewMethod(v As Object) As String
        Return $""{v}""
    End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545829")>
        Public Async Function TestMissingOnImplicitMemberAccess() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        With """"""""
            Dim x = [|.GetHashCode|] Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module",
"Module Program
    Sub Main()
        {|Rename:NewMethod|}()
    End Sub

    Private Sub NewMethod()
        With """"""""
            Dim x = .GetHashCode Xor &H7F3E ' Introduce Local 
        End With
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")>
        Public Async Function TestPreserveCommentsBeforeDeclaration_1() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class Program
    Sub Main(args As String())
        [|Dim one As Program = New Program()
        one.M()
        ' This is the comment
        Dim two As Program = New Program()
        two.M()|]
        one.M()
        two.M()
    End Sub

    Private Sub M()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class Program
    Sub Main(args As String())
        Dim one As Program = Nothing
        Dim two As Program = Nothing
        {|Rename:NewMethod|}(one, two)
        one.M()
        two.M()
    End Sub

    Private Shared Sub NewMethod(ByRef one As Program, ByRef two As Program)
        one = New Program()
        one.M()
        ' This is the comment
        two = New Program()
        two.M()
    End Sub

    Private Sub M()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")>
        Public Async Function TestPreserveCommentsBeforeDeclaration_2() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class Program
    Sub Main(args As String())
        [|Dim one As Program = New Program()
        one.M()
        ' This is the comment
        Dim two As Program = New Program(), three As Program = New Program()
        two.M()|]
        one.M()
        two.M()
        three.M()
    End Sub

    Private Sub M()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class Program
    Sub Main(args As String())
        Dim one As Program = Nothing
        Dim two As Program = Nothing
        Dim three As Program = Nothing
        {|Rename:NewMethod|}(one, two, three)
        one.M()
        two.M()
        three.M()
    End Sub

    Private Shared Sub NewMethod(ByRef one As Program, ByRef two As Program, ByRef three As Program)
        one = New Program()
        one.M()
        ' This is the comment
        two = New Program()
        three = New Program()
        two.M()
    End Sub

    Private Sub M()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")>
        Public Async Function TestPreserveCommentsBeforeDeclaration_3() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class Program
    Sub Main(args As String())
        [|Dim one As Program = New Program()
        one.M()
        ' This is the comment
        Dim two As Program = New Program()
        two.M()
        ' Second Comment
        Dim three As Program = New Program()
        three.M()|]
        one.M()
        two.M()
        three.M()
    End Sub

    Private Sub M()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class Program
    Sub Main(args As String())
        Dim one As Program = Nothing
        Dim two As Program = Nothing
        Dim three As Program = Nothing
        {|Rename:NewMethod|}(one, two, three)
        one.M()
        two.M()
        three.M()
    End Sub

    Private Shared Sub NewMethod(ByRef one As Program, ByRef two As Program, ByRef three As Program)
        one = New Program()
        one.M()
        ' This is the comment
        two = New Program()
        two.M()
        ' Second Comment
        three = New Program()
        three.M()
    End Sub

    Private Sub M()
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/13042")>
        Public Async Function TestTuples() As Task

            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Dim x = (1, 2)|]
        M(x)
    End Sub
    Private Sub M(x As (Integer, Integer))
    End Sub
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace",
"Class Program
    Sub Main(args As String())
        Dim x As (Integer, Integer) = {|Rename:NewMethod|}()
        M(x)
    End Sub

    Private Shared Function NewMethod() As (Integer, Integer)
        Return (1, 2)
    End Function

    Private Sub M(x As (Integer, Integer))
    End Sub
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace")

        End Function

        <Fact, CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11196")>
        Public Async Function TestTupleDeclarationWithNames() As Task

            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Dim x As (a As Integer, b As Integer) = (1, 2)|]
        System.Console.WriteLine(x.a)
    End Sub
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace",
"Class Program
    Sub Main(args As String())
        Dim x As (a As Integer, b As Integer) = {|Rename:NewMethod|}()
        System.Console.WriteLine(x.a)
    End Sub

    Private Shared Function NewMethod() As (a As Integer, b As Integer)
        Return (1, 2)
    End Function
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace")

        End Function

        <Fact, CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11196")>
        Public Async Function TestTupleDeclarationWithSomeNames() As Task

            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Dim x As (a As Integer, Integer) = (1, 2)|]
        System.Console.WriteLine(x.a)
    End Sub
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace",
"Class Program
    Sub Main(args As String())
        Dim x As (a As Integer, Integer) = {|Rename:NewMethod|}()
        System.Console.WriteLine(x.a)
    End Sub

    Private Shared Function NewMethod() As (a As Integer, Integer)
        Return (1, 2)
    End Function
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace")

        End Function

        <Fact, CompilerTrait(CompilerFeature.Tuples)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18311")>
        Public Async Function TestTupleWith1Arity() As Task

            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        Dim y = New ValueTuple(Of Integer)(1)
        [|y.Item1.ToString()|]
    End Sub
End Class
Structure ValueTuple(Of T1)
    Public Property Item1 As T1
    Public Sub New(item1 As T1)
    End Sub
End Structure",
"Class Program
    Sub Main(args As String())
        Dim y = New ValueTuple(Of Integer)(1)
        {|Rename:NewMethod|}(y)
    End Sub

    Private Shared Sub NewMethod(y As ValueTuple(Of Integer))
        y.Item1.ToString()
    End Sub
End Class
Structure ValueTuple(Of T1)
    Public Property Item1 As T1
    Public Sub New(item1 As T1)
    End Sub
End Structure")

        End Function

        <Fact, CompilerTrait(CompilerFeature.Tuples)>
        Public Async Function TestTupleWithInferredNames() As Task
            Await TestAsync(
"Class Program
    Sub Main()
        Dim a = 1
        Dim t = [|(a, b:=2)|]
        System.Console.Write(t.a)
    End Sub
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace",
"Class Program
    Sub Main()
        Dim a = 1
        Dim t = {|Rename:GetT|}(a)
        System.Console.Write(t.a)
    End Sub

    Private Shared Function GetT(a As Integer) As (a As Integer, b As Integer)
        Return (a, b:=2)
    End Function
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace", TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

        End Function

        <Fact, CompilerTrait(CompilerFeature.Tuples)>
        Public Async Function TestTupleWithInferredNames_WithVB15() As Task
            Await TestAsync(
"Class Program
    Sub Main()
        Dim a = 1
        Dim t = [|(a, b:=2)|]
        System.Console.Write(t.a)
    End Sub
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace",
"Class Program
    Sub Main()
        Dim a = 1
        Dim t = {|Rename:GetT|}(a)
        System.Console.Write(t.a)
    End Sub

    Private Shared Function GetT(a As Integer) As (a As Integer, b As Integer)
        Return (a, b:=2)
    End Function
End Class
Namespace System
    Structure ValueTuple(Of T1, T2)
    End Structure
End Namespace", TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitFalse() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).ConfigureAwait(False)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration).ConfigureAwait(False)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(False)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitTrue() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).ConfigureAwait(True)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(True)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitNonLiteral() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).ConfigureAwait(M())|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(M())
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithNoConfigureAwait() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitFalseInLambda() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Run(Async Function () Await Task.Delay(duration).ConfigureAwait(False))|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Run(Async Function() Await Task.Delay(duration).ConfigureAwait(False))
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitFalseDifferentCase() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).configureawait(False)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration).ConfigureAwait(False)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).configureawait(False)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitMixture1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).ConfigureAwait(False)
        Await Task.Delay(duration).ConfigureAwait(True)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration).ConfigureAwait(False)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(False)
        Await Task.Delay(duration).ConfigureAwait(True)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitMixture2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).ConfigureAwait(True)
        Await Task.Delay(duration).ConfigureAwait(False)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration).ConfigureAwait(False)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(True)
        Await Task.Delay(duration).ConfigureAwait(False)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitMixture3() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        [|Await Task.Delay(duration).ConfigureAwait(M())
        Await Task.Delay(duration).ConfigureAwait(False)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await {|Rename:NewMethod|}(duration).ConfigureAwait(False)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(M())
        Await Task.Delay(duration).ConfigureAwait(False)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")>
        Public Async Function TestExtractAsyncMethodWithConfigureAwaitFalseOutsideSelection() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await Task.Delay(duration).ConfigureAwait(False)
        [|Await Task.Delay(duration).ConfigureAwait(True)|]
    End Function
End Class",
"Class C
    Private Async Function MyDelay(duration As TimeSpan) As Task
        Await Task.Delay(duration).ConfigureAwait(False)
        Await {|Rename:NewMethod|}(duration)
    End Function

    Private Shared Async Function NewMethod(duration As TimeSpan) As System.Threading.Tasks.Task
        Await Task.Delay(duration).ConfigureAwait(True)
    End Function
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess1() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?.[|ToString|]()
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As String
        Return b?.ToString()
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess2() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?.[|ToString|]().Length
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As Integer?
        Return b?.ToString().Length
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess3() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?.Count.[|ToString|]()
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As String
        Return b?.Count.ToString()
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess4() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?.[|Count|].ToString()
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As String
        Return b?.Count.ToString()
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess5() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?.[|ToString|]()?.ToString()
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As String
        Return b?.ToString()?.ToString()
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess6() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?.ToString()?.[|ToString|]()
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As String
        Return b?.ToString()?.ToString()
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess7() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = b?[|(0)|]
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new List(of integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As List(Of Integer)) As Integer?
        Return b?(0)
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess8() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new Dictionary(of string, integer)
        dim x = b?![|a|]
    end sub
end class", "
imports System
imports System.Collections.Generic
class C
    sub Test()
        dim b as new Dictionary(of string, integer)
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As Dictionary(Of String, Integer)) As Integer?
        Return b?!a
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess9() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
imports System.Xml.Linq
class C
    sub Test()
        dim b as XElement = nothing
        dim x = b?.[|<e>|]
    end sub
end class", "
imports System
imports System.Collections.Generic
imports System.Xml.Linq
class C
    sub Test()
        dim b as XElement = nothing
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As XElement) As IEnumerable(Of XElement)
        Return b?.<e>
    End Function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")>
        Public Async Function TestConditionalAccess10() As Task
            Await TestInRegularAndScript1Async("
imports System
imports System.Collections.Generic
imports System.Xml.Linq
class C
    sub Test()
        dim b as XElement = nothing
        dim x = b?.[|@e|]
    end sub
end class", "
imports System
imports System.Collections.Generic
imports System.Xml.Linq
class C
    sub Test()
        dim b as XElement = nothing
        dim x = {|Rename:GetX|}(b)
    end sub

    Private Shared Function GetX(b As XElement) As String
        Return b?.@e
    End Function
end class")
        End Function
    End Class
End Namespace
