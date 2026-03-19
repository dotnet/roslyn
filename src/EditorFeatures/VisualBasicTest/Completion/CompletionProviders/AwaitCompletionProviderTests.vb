' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class AwaitCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(AwaitCompletionProvider)
        End Function

        Protected Async Function VerifyAwaitKeyword(markup As String, Optional dotAwait As Boolean = False, Optional dotAwaitf As Boolean = False) As Task
            Dim expectedDescription = If(dotAwait, GetDescription("Await", FeaturesResources.Await_the_preceding_expression), GetDescription("Await", FeaturesResources.Asynchronously_waits_for_the_task_to_finish))
            Await VerifyItemExistsAsync(markup, "Await", expectedDescriptionOrNull:=expectedDescription)
            If dotAwaitf Then
                expectedDescription = String.Format(FeaturesResources.Await_the_preceding_expression_and_add_ConfigureAwait_0, "False")
                Await VerifyItemExistsAsync(markup, "Awaitf", expectedDescriptionOrNull:=expectedDescription)
            Else
                Await VerifyItemIsAbsentAsync(markup, "Awaitf")
            End If
        End Function

        Private Shared Function GetDescription(ByVal keyword As String, ByVal tooltip As String) As String
            Return $"{String.Format(FeaturesResources._0_Keyword, keyword)}{vbCrLf}{tooltip}"
        End Function

        <Fact>
        Public Async Function InSynchronousMethodTest() As Task
            Await VerifyAwaitKeyword("
Class C
     Sub Goo()
        Dim z = $$
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function InMethodStatementTest() As Task
            Await VerifyAwaitKeyword("
Class C
    Async Sub Goo()
        $$
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function InMethodExpressionTest() As Task
            Await VerifyAwaitKeyword("
Class C
    Async Sub Goo()
        Dim z = $$
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function NotInCatchTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function NotInCatchExceptionFilterTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Catch When Err = $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function InCatchNestedDelegateTest() As Task
            Await VerifyAwaitKeyword("
Class C
    Async Sub Goo()
        Try
        Catch
            Dim z = Function() $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function NotInFinallyTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        Try
        Finally
            Dim z = $$
        End Try

    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function NotInSyncLockTest() As Task
            Await VerifyNoItemsExistAsync("
Class C
    Async Sub Goo()
        SyncLock True
            Dim z = $$
        End SyncLock
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitInAsyncSub() As Task
            Await VerifyAwaitKeyword("
Imports System.Threading.Tasks

Class C
    Async Sub Goo()
        Task.CompletedTask.$$
    End Sub
End Class
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitSuggestAfterDotOnTaskOfT() As Task
            Await VerifyAwaitKeyword("
Imports System.Threading.Tasks

Class C
    Private Async Function F(ByVal someTask As Task(Of Integer)) As Task
        someTask.$$
    End Function
End Class
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitSuggestAfterDotOnValueTask() As Task
            Dim valueTaskAssembly = GetType(ValueTask).Assembly.Location

            Await VerifyAwaitKeyword($"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <MetadataReference>{valueTaskAssembly}</MetadataReference>
        <Document FilePath=""Test2.cs"">
Imports System.Threading.Tasks

Class C
    Private Async Function F(ByVal someTask As ValueTask) As Task
        someTask.$$
    End Function
End Class
        </Document>
    </Project>
</Workspace>
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitSuggestAfterDotOnCustomAwaitable() As Task
            Await VerifyAwaitKeyword("
Imports System
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks

Public Class DummyAwaiter
    Implements INotifyCompletion

    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property

    Public Sub OnCompleted(ByVal continuation As Action)
        Return continuation()
    End Sub

    Public Sub GetResult()
    End Sub
End Class

Public Class CustomAwaitable
    Public Function GetAwaiter() As DummyAwaiter
        Return New DummyAwaiter()
    End Function
End Class

Module Program
    Private Async Function Main() As Task
        Dim awaitable = New CustomAwaitable()
        awaitable.$$
    End Function
End Module
", dotAwait:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitSuggestAfterDotBeforeType() As Task
            Await VerifyAwaitKeyword("
Imports System
Imports System.Threading.Tasks

Module Program
    Private Async Function Main(ByVal someTask As Task) As Task
        someTask.$$
        Dim i As Int32 = 0
    End Function
End Module
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitSuggestAfterDotBeforeAnotherAwait() As Task
            Await VerifyAwaitKeyword("
Imports System
Imports System.Threading.Tasks

Module Program
    Private Async Function Main(ByVal someTask As Task) As Task
        someTask.$$
        Await Test()
    End Function

    Private Async Function Test() As Task
    End Function
End Module
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Theory>
        <InlineData("StaticField.$$")>
        <InlineData("StaticProperty.$$")>
        <InlineData("StaticMethod().$$")>
        <InlineData("local.$$")>
        <InlineData("parameter.$$")>
        <InlineData("c.Field.$$")>
        <InlineData("c.Property.$$")>
        <InlineData("c.Method().$$")>
        <InlineData("c.Self.Field.$$")>
        <InlineData("c.Self.Property.$$")>
        <InlineData("c.Self.Method().$$")>
        <InlineData("c.Func()().$$")>
        <InlineData("c(0).$$")>
        <InlineData("Dim t = (CType(c, Task)).$$")>
        <InlineData("Dim t = (TryCast(c, Task)).$$")>
        <InlineData("Dim t = (parameter).$$")>
        <InlineData("Dim t = ((parameter)).$$")>
        <InlineData("Dim t = if(true, parameter, parameter).$$")>
        <InlineData("Dim t = if(null, Task.CompletedTask).$$")>
        Public Async Function DotAwaitSuggestAfterDifferentExpressions(ByVal expression As String) As Task
            Dim t = If(True, expression, expression).Length
            Await VerifyAwaitKeyword($"
Imports System
Imports System.Threading.Tasks

Class C
    Public ReadOnly Property Self As C
        Get
            Return Me
        End Get
    End Property

    Public Field As Task = Task.CompletedTask

    Public Function Method() As Task
        Return Task.CompletedTask
    End Function

    Public ReadOnly Property [Property] As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Default Public ReadOnly Property Item(ByVal i As Integer) As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Public Function Func() As Func(Of Task)
        Return Function() Task.CompletedTask
    End Function
End Class

Module Program
    Shared StaticField As Task = Task.CompletedTask

    Private Shared ReadOnly Property StaticProperty As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Private Function StaticMethod() As Task
        Return Task.CompletedTask
    End Function

    Private Async Function Main(ByVal parameter As Task) As Task
        Dim local As Task = Task.CompletedTask
        Dim c = New C()

        {expression}

    End Function
End Module
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Theory>
        <InlineData("Await Task.Run(Async Function() Task.CompletedTask.$$")>
        <InlineData("Await Task.Run(Async Function() someTask.$$)")>
        <InlineData("
Await Task.Run(Async Function()
                   someTask.$$
               End Function)")>
        <InlineData("
Await Task.Run(Async Function()
                   someTask.$$")>
        <InlineData("Task.Run(Async Function() Await someTask).$$")>
        <InlineData("Await Task.Run(Function() someTask.$$")>
        Public Async Function DotAwaitSuggestInLambdas(lambda As String) As Task
            Await VerifyAwaitKeyword($"
Imports System.Threading.Tasks

Module Program
    Private Async Function Main() As Task
        Dim someTask = Task.CompletedTask

        {lambda}
    End Function
End Module
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitNotAfterDotOnTaskIfAlreadyAwaited() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Threading.Tasks

Class C
    Private Async Function F(ByVal someTask As Task) As Task
        Await someTask.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitNotAfterTaskType() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Task.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitNotInLock() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Threading.Tasks

Class C
    Private Async Function F(ByVal someTask As Task) As Task
        SyncLock Me
            someTask.$$
        End SyncLock
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitQueryNotInSelect() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim z = From t In {Task.CompletedTask} Select t.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitQueryInFirstFromClause() As Task
            Await VerifyAwaitKeyword("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim arrayTask1 = Task.FromResult(new Integer() {})
        Dim qry = From i In arrayTask1.$$
    End Function
End Class
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitQueryNotInSecondFromClause_1() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim array1 = new Integer() {}
        Dim arrayTask2 = Task.FromResult(new Integer() {})

        Dim qry = From i1 In array1
                  From i2 In arrayTask2.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitQueryNotInSecondFromClause_2() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim array1 = new Integer() {}
        Dim arrayTask2 = Task.FromResult(new Integer() {})

        Dim qry = From i1 In array1, i2 In arrayTask2.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitQueryNotInSecondFromClause_3() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim array1 = new Integer() {}
        Dim arrayTask2 = Task.FromResult(new Integer() {})

        Dim qry = From i1 In array1
                  From i2 In array1, i2 In arrayTask2.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitQueryNotInSecondFromClause_4() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim array1 = new Integer() {}
        Dim arrayTask2 = Task.FromResult(new Integer() {})

        Dim qry = From i1 In array1, i2 In array1
                  From i3 In arrayTask2.$$
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function DotAwaitQueryInJoinClause() As Task
            Await VerifyAwaitKeyword("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim array1 = new Integer() {}
        Dim arrayTask2 = Task.FromResult(new Integer() {})

        Dim qry = From i1 In array1
                  Join i2 In arrayTask2.$$
                  On i1 Equals i2
    End Function
End Class
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitQueryInGroupJoinClause() As Task
            Await VerifyAwaitKeyword("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Private Async Function F() As Task
        Dim array1 = new Integer() {}
        Dim arrayTask2 = Task.FromResult(new Integer() {})

        Dim qry = From i1 In array1
                  Group Join i2 In arrayTask2.$$
                  On i1 Equals i2 Into g = Group
    End Function
End Class
", dotAwait:=True, dotAwaitf:=True)
        End Function

        <Fact>
        Public Async Function DotAwaitNotAfterConditionalAccessOfTaskMembers() As Task
            Await VerifyNoItemsExistAsync("
Imports System.Threading.Tasks

Class C
    Private Async Function F(ByVal someTask As Task) As Task
        someTask?.$$
    End Function
End Class
")
        End Function

        <Theory>
        <InlineData("c?.SomeTask.$$")>
        <InlineData("c.M()?.SomeTask.$$")>
        <InlineData("c.Pro?.SomeTask.$$")>
        <InlineData("c?.M().SomeTask.$$")>
        <InlineData("c?.Pro.SomeTask.$$")>
        <InlineData("c?.M()?.SomeTask.$$")>
        <InlineData("c?.Pro?.SomeTask.$$")>
        <InlineData("c.M()?.Pro.SomeTask.$$")>
        <InlineData("c.Pro?.M().SomeTask.$$")>
        <InlineData("c.M()?.M().M()?.M().SomeTask.$$")>
        <InlineData("new C().M()?.Pro.M()?.M().SomeTask.$$")>
        Public Async Function DotAwaitNotAfterDotInConditionalAccessChain(ByVal conditionalAccess As String) As Task
            Await VerifyNoItemsExistAsync($"
Imports System.Threading.Tasks

Public Class C
    Public ReadOnly Property SomeTask As Task
        Get
            Return Task.CompletedTask
        End Get
    End Property

    Public ReadOnly Property Pro As C
        Get
            Return Me
        End Get
    End Property

    Public Function M() As C
        Return Me
    End Function
End Class

Module Program
    Async Function Main() As Task
        Dim c = New C()

        If True Then
            {conditionalAccess}
        End If
    End Function
End Module
")
        End Function
    End Class
End Namespace
