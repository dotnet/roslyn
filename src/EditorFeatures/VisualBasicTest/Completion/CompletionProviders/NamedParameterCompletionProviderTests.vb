' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class NamedParameterCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(NamedParameterCompletionProvider)
        End Function

        <Fact>
        Public Async Function TestInObjectCreation() As Task
            Await VerifyItemExistsAsync(
<Text>
Class Goo
	Public Sub New(Optional a As Integer = 42)
	End Sub

	Private Sub Bar()
		Dim b = New Goo($$
	End Sub
End Class
</Text>.Value, "a", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestInBaseConstructor() As Task
            Await VerifyItemExistsAsync(
<Text>
Class Goo
	Public Sub New(Optional a As Integer = 42)
	End Sub
End Class

Class FogBar
	Inherits Goo
	Public Sub New(b As Integer)
		MyBase.New($$
	End Sub
End Class
</Text>.Value, "a", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestAttributeConstructor() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
Class Goo
Class TestAttribute
	Inherits Attribute
	Public Sub New(Optional a As Integer = 42)
	End Sub
End Class

&lt;Test($$)&gt; _
Class Goo
End Class
</Text>.Value, "a", displayTextSuffix:=":=")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546190")>
        Public Async Function TestAttributeNamedParameter1() As Task
            Await VerifyItemExistsAsync(
<Text>
Class SomethingAttribute
    Inherits System.Attribute
    Public x As Integer
End Class

&lt;Something($$)&gt; ' type x in the parens
Class D
End Class
</Text>.Value, "x", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestAttributeConstructorAfterComma() As Task
            Await VerifyItemIsAbsentAsync(
<Text>
Class TestAttribute
    Inherits Attribute
    Public Sub New(Optional a As Integer = 42, Optional s As String = """")
    End Sub
End Class

&lt;Test(s:="""",$$  &gt; _
Class Goo
End Class
</Text>.Value, "a", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestInvocationExpression() As Task
            Await VerifyItemExistsAsync(
<Text>
Class Goo
	Private Sub Bar(a As Integer)
		Bar($$
	End Sub
End Class
</Text>.Value, "a", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestInvocationExpressionAfterComma() As Task
            Await VerifyItemExistsAsync(
<Text>
Class Goo
	Private Sub Bar(a As Integer, b as String)
		Bar(b:="""", $$
	End Sub
End Class
</Text>.Value, "a", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestInIndexers() As Task
            Await VerifyItemExistsAsync(
<Text>
Class Test
    Default Public ReadOnly Property Item(i As Integer) As Integer
        Get
            Return i
        End Get
    End Property
End Class

Module TestModule
    Sub Main()
        Dim x As Test = New Test()
        Dim y As Integer

        y = x($$
    End Sub
End Module
</Text>.Value, "i", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestInDelegates() As Task
            Await VerifyItemExistsAsync(
<Text>
Delegate Sub SimpleDelegate(x As Integer)
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        Dim d As SimpleDelegate = AddressOf F
        d($$
    End Sub
End Module
</Text>.Value, "x", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestInDelegateInvokeSyntax() As Task
            Await VerifyItemExistsAsync(
<Text>
Delegate Sub SimpleDelegate(x As Integer)
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        Dim d As SimpleDelegate = AddressOf F
        d.Invoke($$
    End Sub
End Module
</Text>.Value, "x", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestNotAfterColonEquals() As Task
            Await VerifyNoItemsExistAsync(
<Text>
Class Goo
    Private Sub Bar(a As Integer, b As String)
        Bar(a:=$$
    End Sub
End Class
</Text>.Value)
        End Function

        <Fact>
        Public Async Function TestNotInCollectionInitializers() As Task
            Await VerifyNoItemsExistAsync(
<Text>
Class Goo
    Private Sub Bar(a As Integer, b As String)
        Dim numbers = {1, 2, 3,$$ 4, 5}
    End Sub
End Class
</Text>.Value)
        End Function

        <Fact>
        Public Async Function TestDoNotFilterYet() As Task
            Dim markup =
<Text>
Class Class1
    Private Sub Test()
        Goo(str:="""", $$)
    End Sub

    Private Sub Goo(str As String, character As Char)
    End Sub

    Private Sub Goo(str As String, bool As Boolean)
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "bool", displayTextSuffix:=":=")
            Await VerifyItemExistsAsync(markup, "character", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestMethodOverloads() As Task
            Dim markup =
<Text>
Class Goo
    Private Sub Test()
        Dim m As Object = Nothing
        Method(obj:=m, $$
    End Sub

    Private Sub Method(obj As Object, num As Integer, str As String)
    End Sub
    Private Sub Method(num As Integer, b As Boolean, str As String)
    End Sub
    Private Sub Method(obj As Object, b As Boolean, str As String)
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "str", displayTextSuffix:=":=")
            Await VerifyItemExistsAsync(markup, "num", displayTextSuffix:=":=")
            Await VerifyItemExistsAsync(markup, "b", displayTextSuffix:=":=")
        End Function

        <Fact>
        Public Async Function TestExistingNamedParamsAreFilteredOut() As Task
            Dim markup =
<Text>
Class Goo
    Private Sub Test()
        Dim m As Object = Nothing
        Method(obj:=m, str:="""", $$);
    End Sub

    Private Sub Method(obj As Object, num As Integer, str As String)
    End Sub
    Private Sub Method(dbl As Double, str As String)
    End Sub
    Private Sub Method(num As Integer, b As Boolean, str As String)
    End Sub
    Private Sub Method(obj As Object, b As Boolean, str As String)
    End Sub
End Class
</Text>.Value

            Await VerifyItemExistsAsync(markup, "num", displayTextSuffix:=":=")
            Await VerifyItemExistsAsync(markup, "b", displayTextSuffix:=":=")
            Await VerifyItemIsAbsentAsync(markup, "obj", displayTextSuffix:=":=")
            Await VerifyItemIsAbsentAsync(markup, "str", displayTextSuffix:=":=")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529370")>
        <WpfFact(Skip:="529370")>
        Public Async Function TestKeywordAsEscapedIdentifier() As Task
            Await VerifyItemExistsAsync(
<Text>
Class Goo
    Private Sub Bar([Boolean] As Boolean)
        Bar($$
    End Sub
End Class
</Text>.Value, "[Boolean]", displayTextSuffix:=":=")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546589")>
        Public Async Function TestCommitOnEquals() As Task
            Dim text = <Text>
Module Program
    Sub Main(args As String())
        Main(a$$
    End Sub
End Module

</Text>.Value

            Dim expected = <Text>
Module Program
    Sub Main(args As String())
        Main(args:=
    End Sub
End Module

</Text>.Value

            Await VerifyProviderCommitAsync(text, "args:=", expected, "="c)
        End Function

        <Fact>
        Public Async Function TestCommitOnColon() As Task
            Dim text = <Text>
Module Program
    Sub Main(args As String())
        Main(a$$
    End Sub
End Module

</Text>.Value

            Dim expected = <Text>
Module Program
    Sub Main(args As String())
        Main(args:
    End Sub
End Module

</Text>.Value

            Await VerifyProviderCommitAsync(text, "args:=", expected, ":"c)
        End Function

        <Fact>
        Public Async Function TestCommitOnSpace() As Task
            Dim text = <Text>
Module Program
    Sub Main(args As String())
        Main(a$$
    End Sub
End Module

</Text>.Value

            Dim expected = <Text>
Module Program
    Sub Main(args As String())
        Main(args:= 
    End Sub
End Module

</Text>.Value

            Await VerifyProviderCommitAsync(text, "args:=", expected, " "c)
        End Function

        <Fact>
        Public Async Function TestNotInTrivia() As Task
            Await VerifyNoItemsExistAsync(
<Text>
Class Goo
	Private Sub Bar(a As Integer)
		Bar(a:=1 ' $$
	End Sub
End Class
</Text>.Value)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041260")>
        Public Async Function TestConditionalInvocation() As Task
            Await VerifyItemExistsAsync(
<Text>
Imports System
Class Goo
    Private Sub Bar(a As Integer)
        Dim x as Action(Of Integer) = Nothing
        x?($$
    End Sub
End Class
</Text>.Value, "obj", displayTextSuffix:=":=")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")>
        Public Async Function TestExclusivityCheckAfterComma() As Task
            Await VerifyAnyItemExistsAsync(
<Text>
Imports System
Class Goo
    Private Sub Bar(a As Integer)
        Bar(1, 2, $$)
    End Sub
End Class
</Text>.Value)
        End Function
    End Class
End Namespace

