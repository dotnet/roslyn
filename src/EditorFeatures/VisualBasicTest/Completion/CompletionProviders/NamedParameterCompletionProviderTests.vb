' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class NamedParameterCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New NamedParameterCompletionProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InObjectCreation()
            VerifyItemExists(
<Text>
Class Foo
	Public Sub New(Optional a As Integer = 42)
	End Sub

	Private Sub Bar()
		Dim b = New Foo($$
	End Sub
End Class
</Text>.Value, "a:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InBaseConstructor()
            VerifyItemExists(
<Text>
Class Foo
	Public Sub New(Optional a As Integer = 42)
	End Sub
End Class

Class FogBar
	Inherits Foo
	Public Sub New(b As Integer)
		MyBase.New($$
	End Sub
End Class
</Text>.Value, "a:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeConstructor()
            VerifyItemIsAbsent(
<Text>
Class Foo
Class TestAttribute
	Inherits Attribute
	Public Sub New(Optional a As Integer = 42)
	End Sub
End Class

&lt;Test($$)&gt; _
Class Foo
End Class
</Text>.Value, "a:=")
        End Sub

        <WorkItem(546190)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNamedParameter1()
            VerifyItemExists(
<Text>
Class SomethingAttribute
    Inherits System.Attribute
    Public x As Integer
End Class

&lt;Something($$)&gt; ' type x in the parens
Class D
End Class
</Text>.Value, "x:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeConstructorAfterComma()
            VerifyItemIsAbsent(
<Text>
Class TestAttribute
    Inherits Attribute
    Public Sub New(Optional a As Integer = 42, Optional s As String = """")
    End Sub
End Class

&lt;Test(s:="""",$$  &gt; _
Class Foo
End Class
</Text>.Value, "a:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvocationExpression()
            VerifyItemExists(
<Text>
Class Foo
	Private Sub Bar(a As Integer)
		Bar($$
	End Sub
End Class
</Text>.Value, "a:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvocationExpressionAfterComma()
            VerifyItemExists(
<Text>
Class Foo
	Private Sub Bar(a As Integer, b as String)
		Bar(b:="""", $$
	End Sub
End Class
</Text>.Value, "a:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InIndexers()
            VerifyItemExists(
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
</Text>.Value, "i:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InDelegates()
            VerifyItemExists(
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
</Text>.Value, "x:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InDelegateInvokeSyntax()
            VerifyItemExists(
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
</Text>.Value, "x:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotAfterColonEquals()
            VerifyNoItemsExist(
<Text>
Class Foo
    Private Sub Bar(a As Integer, b As String)
        Bar(a:=$$
    End Sub
End Class
</Text>.Value)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInCollectionInitializers()
            VerifyNoItemsExist(
<Text>
Class Foo
    Private Sub Bar(a As Integer, b As String)
        Dim numbers = {1, 2, 3,$$ 4, 5}
    End Sub
End Class
</Text>.Value)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DontFilterYet()
            Dim markup =
<Text>
Class Class1
    Private Sub Test()
        Foo(str:="""", $$)
    End Sub

    Private Sub Foo(str As String, character As Char)
    End Sub

    Private Sub Foo(str As String, bool As Boolean)
    End Sub
End Class
</Text>.Value

            VerifyItemExists(markup, "bool:=")
            VerifyItemExists(markup, "character:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MethodOverloads()
            Dim markup =
<Text>
Class Foo
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

            VerifyItemExists(markup, "str:=")
            VerifyItemExists(markup, "num:=")
            VerifyItemExists(markup, "b:=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExistingNamedParamsAreFilteredOut()
            Dim markup =
<Text>
Class Foo
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

            VerifyItemExists(markup, "num:=")
            VerifyItemExists(markup, "b:=")
            VerifyItemIsAbsent(markup, "obj:=")
            VerifyItemIsAbsent(markup, "str:=")
        End Sub

        <WorkItem(529370)>
        <WpfFact(Skip:="529370"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordAsEscapedIdentifier()
            VerifyItemExists(
<Text>
Class Foo
    Private Sub Bar([Boolean] As Boolean)
        Bar($$
    End Sub
End Class
</Text>.Value, "[Boolean]:=")
        End Sub

        <WorkItem(546589)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnEquals()
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

            VerifyProviderCommit(text, "args:=", expected, "="c, Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnColon()
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

            VerifyProviderCommit(text, "args:=", expected, ":"c, Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnSpace()
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

            VerifyProviderCommit(text, "args:=", expected, " "c, Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInTrivia()
            VerifyNoItemsExist(
<Text>
Class Foo
	Private Sub Bar(a As Integer)
		Bar(a:=1 ' $$
	End Sub
End Class
</Text>.Value)
        End Sub

        <WorkItem(1041260)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConditionalInvocation()
            VerifyItemExists(
<Text>
Imports System
Class Foo
    Private Sub Bar(a As Integer)
        Dim x as Action(Of Integer) = Nothing
        x?($$
    End Sub
End Class
</Text>.Value, "obj:=")
        End Sub

        <WorkItem(1040247)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ExclusivityCheckAfterComma()
            VerifyAnyItemExists(
<Text>
Imports System
Class Foo
    Private Sub Bar(a As Integer)
        Bar(1, 2, $$)
    End Sub
End Class
</Text>.Value)
        End Sub
    End Class
End Namespace

