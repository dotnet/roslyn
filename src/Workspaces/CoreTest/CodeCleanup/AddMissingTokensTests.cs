// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
public sealed class AddMissingTokensTests
{
    [Fact]
    public async Task MultipleLineIfStatementThen()
    {
        await VerifyAsync(CreateMethod(@"[|
        If True
            Dim a = 1
        End If|]"), CreateMethod(@"
        If True Then
            Dim a = 1
        End If"));
    }

    [Fact]
    public async Task TypeArgumentOf()
    {
        await VerifyAsync(CreateMethod(@"[|
        Dim a As List(Integer)|]"), CreateMethod(@"
        Dim a As List(Of Integer)"));
    }

    [Fact]
    public async Task TypeParameterOf()
    {
        await VerifyAsync(@"[|Class A(T)
End Class|]", @"Class A(Of T)
End Class");
    }

    [Fact]
    public async Task MethodDeclaration()
    {
        await VerifyAsync(@"Class A
    [|Sub Test
    End Sub|]
End Class", @"Class A
    Sub Test()
    End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544318")]
    public async Task MethodInvocation_TypeArgParens()
    {
        await VerifyAsync(@"[|Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

Module Program
    Sub Main(args As String())
        [|Dim q = New List(Of Thread

    |]End Sub|]", @"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Thread

    End Sub");
    }

    [Fact]
    public async Task MethodInvocation_Sub()
    {
        await VerifyAsync(@"Class A
    Sub Test
        [|Test|]
    End Sub
End Class", @"Class A
    Sub Test
        Test()
    End Sub
End Class");
    }

    [Fact]
    public async Task MethodInvocation_Function()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        [|Test|]
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Test()
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task IdentifierMethod_Return()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        Return [|Test2|]
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Return Test2()
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task IdentifierMethod_Assign()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        Dim a = [|Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Dim a = Test2()
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task IdentifierMethod_DotName_DoNotAdd()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        Dim a = [|Me.Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Dim a = Me.Test2
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task MethodInvocation_DotName()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        [|Me.Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Me.Test2()
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task MethodInvocation_Generic()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        [|Me.Test2(Of Integer)|]
        Return 1
    End Function

    Function Test2(Of T) As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Me.Test2(Of Integer)()
        Return 1
    End Function

    Function Test2(Of T) As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task MethodInvocation_Call()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        Call [|Me.Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Call Me.Test2()
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task EventHandler_AddressOf1()
    {
        await VerifyAsync(@"Class A
    Sub EventMethod()
        [|AddHandler TestEvent, AddressOf EventMethod|]
    End Sub

    Public Event TestEvent()
End Class", @"Class A
    Sub EventMethod()
        AddHandler TestEvent, AddressOf EventMethod
    End Sub

    Public Event TestEvent()
End Class");
    }

    [Fact]
    public async Task EventHandler_AddressOf2()
    {
        await VerifyAsync(@"Class A
    Sub EventMethod()
        [|RemoveHandler TestEvent, AddressOf EventMethod|]
    End Sub

    Public Event TestEvent()
End Class", @"Class A
    Sub EventMethod()
        RemoveHandler TestEvent, AddressOf EventMethod
    End Sub

    Public Event TestEvent()
End Class");
    }

    [Fact]
    public async Task Delegate_AddressOf()
    {
        await VerifyAsync(@"Class A
    Sub Method()
        [|Dim a As Action = New Action(AddressOf Method)|]
    End Sub
End Class", @"Class A
    Sub Method()
        Dim a As Action = New Action(AddressOf Method)
    End Sub
End Class");
    }

    [Fact]
    public async Task EventDeclaration()
    {
        await VerifyAsync(@"Class A
    Sub EventMethod()
        [|RaiseEvent TestEvent|]
    End Sub

    Public Event TestEvent()
End Class", @"Class A
    Sub EventMethod()
        RaiseEvent TestEvent()
    End Sub

    Public Event TestEvent()
End Class");
    }

    [Fact]
    public async Task RaiseEvent()
    {
        await VerifyAsync(@"Class A
    Sub EventMethod()
        RaiseEvent TestEvent
    End Sub

    [|Public Event TestEvent|]
End Class", @"Class A
    Sub EventMethod()
        RaiseEvent TestEvent
    End Sub

    Public Event TestEvent()
End Class");
    }

    [Fact]
    public async Task DelegateInvocation()
    {
        await VerifyAsync(CreateMethod(@"
        Dim a As Action
        [|a|]"), CreateMethod(@"
        Dim a As Action
        a()"));
    }

    [Fact]
    public async Task Attribute()
    {
        await VerifyAsync(@"[|<Obsolete>
Class C(Of T)
    Sub Main
        Dim a = {1, 2, 3}
    End Sub
End Class|]", @"<Obsolete>
Class C(Of T)
    Sub Main()
        Dim a = {1, 2, 3}
    End Sub
End Class");
    }

    [Fact]
    public async Task ObjectCreation()
    {
        await VerifyAsync(@"[|Module Program
    Sub Main(args As String())
        Dim x As New ClassInNewFile
        Dim c As CustomClass = New CustomClass(""constructor"")
    End Sub
End Module

Friend Class CustomClass
    Sub New(p1 As String)
    End Sub
End Class

Class ClassInNewFile
End Class|]", @"Module Program
    Sub Main(args As String())
        Dim x As New ClassInNewFile
        Dim c As CustomClass = New CustomClass(""constructor"")
    End Sub
End Module

Friend Class CustomClass
    Sub New(p1 As String)
    End Sub
End Class

Class ClassInNewFile
End Class");
    }

    [Fact]
    public async Task Constructor()
    {
        await VerifyAsync(@"[|Class C
    Sub New
    End Sub
End Class|]", @"Class C
    Sub New()
    End Sub
End Class");
    }

    [Fact]
    public async Task DeclareStatement()
    {
        await VerifyAsync(@"[|Class C
    Declare Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA"" 
End Class|]", @"Class C
    Declare Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA"" ()
End Class");
    }

    [Fact]
    public async Task DelegateStatement()
    {
        await VerifyAsync(@"[|Class C
    Delegate Sub Test
End Class|]", @"Class C
    Delegate Sub Test()
End Class");
    }

    [Fact]
    public async Task MethodStatementWithComment()
    {
        await VerifyAsync(@"[|Class C
    [|Sub Test ' test
    End Sub|]
End Class|]", @"Class C
    Sub Test() ' test
    End Sub
End Class");
    }

    [Fact]
    public async Task MultipleLineIfStatementThenWithComment()
    {
        await VerifyAsync(CreateMethod(@"[|
        If True ' test
            Dim a = 1
        End If|]"), CreateMethod(@"
        If True Then ' test
            Dim a = 1
        End If"));
    }

    [Fact]
    public async Task TypeArgumentOf_Comment_DoNotAdd()
    {

        // parser doesn't recognize the broken list as Type Argument List
        await VerifyAsync(CreateMethod(@"[|
        Dim a As List( ' test
                      Integer)|]"), CreateMethod(@"
        Dim a As List( ' test
                      Integer)"));
    }

    [Fact]
    public async Task TypeParameterOf_Comment()
    {
        await VerifyAsync(@"[|Class A( ' test
                                   T)
End Class|]", @"Class A(Of ' test
                                   T)
End Class");
    }

    [Fact]
    public async Task MethodInvocation_Function_Comment()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        [|Test ' test|]
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Test() ' test
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task IdentifierMethod_Return_Comment()
    {
        await VerifyAsync(@"Class A
    Function Test() As Integer
        Return [|Test2 ' test|]
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class", @"Class A
    Function Test() As Integer
        Return Test2() ' test
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class");
    }

    [Fact]
    public async Task ImplementsClause()
    {
        await VerifyAsync(@"Class Program
    Implements I
    [|Public Sub Method() Implements I.Method
    End Sub|]
End Class

Interface I
    Sub Method()
End Interface", @"Class Program
    Implements I
    Public Sub Method() Implements I.Method
    End Sub
End Class

Interface I
    Sub Method()
End Interface");
    }

    [Fact]
    public async Task OperatorStatement()
    {
        await VerifyAsync(@"[|Public Structure abc
    Public Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
        Dim r As New abc
        ' Insert code to calculate And of x and y.
        Return r
    End Operator
End Structure|]", @"Public Structure abc
    Public Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
        Dim r As New abc
        ' Insert code to calculate And of x and y.
        Return r
    End Operator
End Structure");
    }

    [Fact]
    public async Task PropertyAndAccessorStatement()
    {
        await VerifyAsync(@"[|Class Class1
    Private propertyValue As String
    Public Property prop1() As String
        Get
            Return propertyValue
        End Get
        Set(ByVal value As String)
            propertyValue = value
        End Set
    End Property
End Class|]", @"Class Class1
    Private propertyValue As String
    Public Property prop1() As String
        Get
            Return propertyValue
        End Get
        Set(ByVal value As String)
            propertyValue = value
        End Set
    End Property
End Class");
    }

    [Fact]
    public async Task LambdaExpression()
    {
        await VerifyAsync(@"[|Class Class1
    Dim f as Action = Sub()
                      End Sub
End Class|]", @"Class Class1
    Dim f as Action = Sub()
                      End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544225")]
    public async Task StructuredTrivia_Expression_DoNotCrash()
    {
        await VerifyAsync(@"[|#Const Goo1 = 1
#Const Goo2 = 2
#If Goo1 Then
#ElseIf Goo2 Then
#Else
#End If|]", @"#Const Goo1 = 1
#Const Goo2 = 2
#If Goo1 Then
#ElseIf Goo2 Then
#Else
#End If");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544169")]
    public async Task EventStatement_AsClause()
    {
        await VerifyAsync(@"[|Imports System.ComponentModel
Class Goo
    Public Event PropertyChanged As PropertyChangedEventHandler
End Class|]", @"Imports System.ComponentModel
Class Goo
    Public Event PropertyChanged As PropertyChangedEventHandler
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544167")]
    public async Task InvocationExpression_NoParenthesesForPredefinedCastExpression()
    {
        await VerifyAsync(@"[|Class Program
    Sub Main(args As String())
        CInt(5)
    End Sub
End Class|]", @"Class Program
    Sub Main(args As String())
        CInt(5)
    End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544178")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544317")]
    public async Task ObjectCreationExpression()
    {
        await VerifyAsync(@"[|Class C
    Function F() As C
        Return New C
    End Function
End Class|]", @"Class C
    Function F() As C
        Return New C
    End Function
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544317")]
    public async Task ObjectCreationExpression_Initializer()
    {
        await VerifyAsync(@"[|Public Class SomeClass
    Public goo As Integer

    Sub SomeSub()
        [|Dim c = New SomeClass With {.goo = 23}|]
    End Sub
End Class|]", @"Public Class SomeClass
    Public goo As Integer

    Sub SomeSub()
        Dim c = New SomeClass With {.goo = 23}
    End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544178")]
    public async Task ObjectCreationExpression_GenericName()
    {
        await VerifyAsync(@"[|Imports System

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Integer
    End Sub
End Module|]", @"Imports System

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Integer
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544178")]
    public async Task ObjectCreationExpression_AsNewClause()
    {
        await VerifyAsync(@"[|Class C
    Dim a As New C
End Class|]", @"Class C
    Dim a As New C
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544301")]
    public async Task ContinueStatement_While()
    {
        await VerifyAsync(@"Module M
    Sub S()
        [|While True
            Continue
        End While|]
    End Sub
End Module", @"Module M
    Sub S()
        While True
            Continue While
        End While
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544301")]
    public async Task ContinueStatement_For()
    {
        await VerifyAsync(@"Module M
    Sub S()
        [|For i = 1 to 10
            Continue
        Next|]
    End Sub
End Module", @"Module M
    Sub S()
        For i = 1 to 10
            Continue For
        Next
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544380")]
    public async Task IfDirective()
    {
        await VerifyAsync(@"[|#If VBC_VER >= 9.0

Class C
End Class|]", @"#If VBC_VER >= 9.0 Then

Class C
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544386")]
    public async Task NamedFieldInitializer()
    {
        await VerifyAsync(@"[|Class S
    Public Sub Goo()
    End Sub
    Property X
    Sub test()
        Dim x = New S With {.X = 0,.Goo}
    End Sub
End Class|]", @"Class S
    Public Sub Goo()
    End Sub
    Property X
    Sub test()
        Dim x = New S With {.X = 0, .Goo}
    End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544526")]
    public async Task DoNotCrash_ImplementsStatement()
    {
        await VerifyAsync(@"[|Class C
    Sub Main() 
        Implements IDisposable.Dispose
    End Sub
End Class|]", @"Class C
    Sub Main()
        Implements IDisposable.Dispose
    End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544525")]
    public async Task AccessorStatement_AddRemoveHandler_RaiseEvent()
    {
        await VerifyAsync(@"[|Class C
    Public Custom Event E1 As Action
        AddHandler
        End AddHandler
        RemoveHandler
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
End Class|]", @"Class C
    Public Custom Event E1 As Action
        AddHandler()
        End AddHandler
        RemoveHandler()
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545176")]
    public async Task CallStatement_Lambda()
    {
        await VerifyAsync(@"[|Module Program
    Sub Main()
        Call Sub() Console.WriteLine(1)
    End Sub
End Module|]", @"Module Program
    Sub Main()
        Call Sub() Console.WriteLine(1)
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545256")]
    public async Task HandlesClauseItem_DoNotAddParentheses()
    {
        await VerifyAsync(@"[|Structure s1
    Sub Goo() Handles Me.Goo
 
    End Sub
End Structure|]", @"Structure s1
    Sub Goo() Handles Me.Goo

    End Sub
End Structure");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545380")]
    public async Task DoNotAddParenthesesInForEachControlVariable()
    {
        await VerifyAsync(@"[|Module Module1
    Sub Main()
        For Each goo in {} 
    End Sub
 
    Sub Goo()
    End Sub
End Module|]", @"Module Module1
    Sub Main()
        For Each goo in {}
    End Sub

    Sub Goo()
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545380")]
    public async Task DoNotAddParenthesesInForControlVariable()
    {
        await VerifyAsync(@"[|Module Module1
    Sub Main()
        For goo to 
    End Sub
 
    Sub Goo()
    End Sub
End Module|]", @"Module Module1
    Sub Main()
        For goo to 
    End Sub

    Sub Goo()
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545483")]
    public async Task DoNotAddParenthesesForMissingName()
    {
        await VerifyAsync(@"[|Class C
    Public Overrides Function|]", @"Class C
    Public Overrides Function");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545483")]
    public async Task CombinedDelegates()
    {
        await VerifyAsync(@"[|Imports System
Class A
    Public Shared Operator +(x As A, y As A) As Action
    End Operator
    Shared Sub Main()
        Dim x As New A
        Call x + x
    End Sub
End Class|]", @"Imports System
Class A
    Public Shared Operator +(x As A, y As A) As Action
    End Operator
    Shared Sub Main()
        Dim x As New A
        Call x + x
    End Sub
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546581")]
    public async Task ThenOmittedWithSurroundingErrors()
    {
        await VerifyAsync(CreateMethod(@"[|
        If True OrElse|]"), CreateMethod(@"
        If True OrElse"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546581")]
    public async Task ThenOmittedWithSurroundingErrors1()
    {
        await VerifyAsync(CreateMethod(@"[|
        If True|]"), CreateMethod(@"
        If True Then"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546797")]
    public async Task ParenthesisWithLineContinuation()
    {
        await VerifyAsync(CreateMethod(@"[|
            System.Diagnostics.Debug.Assert _ (True)|]"), CreateMethod(@"
        System.Diagnostics.Debug.Assert _ (True)"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546806")]
    public async Task ThenWithLineContinuation()
    {
        await VerifyAsync(CreateMethod(@"[|
#If Condition _ Then
            ' blah
#End If|]"), CreateMethod(@"
#If Condition _ Then
        ' blah
#End If"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531278")]
    public async Task ThenInIfDirective()
    {
        await VerifyAsync(@"#Const ccConst = 0
[|#If ccConst
#End If|]
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
    End Sub
End Module", @"#Const ccConst = 0
#If ccConst Then
#End If
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607792")]
    public async Task CaseKeywordInSelectStatement()
    {
        await VerifyAsync(@"
Module Program
    Sub Main()
[|
        Select 1
        End Select

        Dim z = Function() From x In """"
        :Select 1
        End Select

        Dim z2 = Function() From x In """" : Select        1
        End Select
|]
    End Sub
End Module", @"
Module Program
    Sub Main()

        Select Case 1
        End Select

        Dim z = Function() From x In """"
        : Select Case 1
        End Select

        Dim z2 = Function() From x In """" : Select Case 1
        End Select

    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530789")]
    public async Task Bug530789()
    {
        await VerifyAsync(@"Imports System
Module Program
    Sub Main()
        [|If True Then Console.WriteLine else If False Then Console.WriteLine else Console.writeline|]
    End Sub
End Module", @"Imports System
Module Program
    Sub Main()
        If True Then Console.WriteLine() else If False Then Console.WriteLine() else Console.writeline()
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530039")]
    public async Task TestArraySyntax()
    {
        await VerifyAsync(@"[|Module TestMod
Sub Main()
Dim y As Object
Dim x As cls2(Of y.gettype())
End Sub
End Module|]", @"Module TestMod
    Sub Main()
        Dim y As Object
        Dim x As cls2(Of y.gettype())
    End Sub
End Module");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithoutAsClause()
    {
        await VerifyAsync(@"[|
Interface I
    Function Goo() As System.Threading.Tasks.Task
End Interface

Class Test
    Implements I

    '   a. Basic
    Async Function Goo1()
    End Function

    '   b. With Trailing trivia
    Async Function Goo2()   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo4
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo5() Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo6()      Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo7()
    End Sub

    '   g. Without End Function
    Async Function Goo3()

    '   h. With End Function On SameLine
    Async Function Goo4(ByVal x As Integer) End Function

End Class|]", @"
Interface I
    Function Goo() As System.Threading.Tasks.Task
End Interface

Class Test
    Implements I

    '   a. Basic
    Async Function Goo1() As System.Threading.Tasks.Task
    End Function

    '   b. With Trailing trivia
    Async Function Goo2() As System.Threading.Tasks.Task   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo4() As System.Threading.Tasks.Task
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo5() As System.Threading.Tasks.Task Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo6() As System.Threading.Tasks.Task Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo7()
    End Sub

    '   g. Without End Function
    Async Function Goo3() As System.Threading.Tasks.Task

    '   h. With End Function On SameLine
    Async Function Goo4(ByVal x As Integer) As System.Threading.Tasks.Task End Function

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithoutAsClause_WithAddedImports()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks

Interface I
    Function Goo() As Task
End Interface

Class Test
    Implements I

    '   a. Basic
    Async Function Goo1()
    End Function

    '   b. With Trailing trivia
    Async Function Goo2()   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo4
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo5() Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo6()      Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo7()
    End Sub

    '   g. Without End Function
    Async Function Goo3()

    '   h. With End Function On SameLine
    Async Function Goo4(ByVal x As Integer) End Function

End Class|]", @"
Imports System
Imports System.Threading.Tasks

Interface I
    Function Goo() As Task
End Interface

Class Test
    Implements I

    '   a. Basic
    Async Function Goo1() As Task
    End Function

    '   b. With Trailing trivia
    Async Function Goo2() As Task   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo4() As Task
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo5() As Task Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo6() As Task Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo7()
    End Sub

    '   g. Without End Function
    Async Function Goo3() As Task

    '   h. With End Function On SameLine
    Async Function Goo4(ByVal x As Integer) As Task End Function

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithoutAsClause()
    {
        await VerifyAsync(@"[|
Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class Test
    Implements I

    '   a. Basic
    Iterator Function Goo1()
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2()   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo4
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo5() Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo6()      Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo7()
    End Sub

    '   g. Without End Function
    Iterator Function Goo3()

End Class|]", @"
Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class Test
    Implements I

    '   a. Basic
    Iterator Function Goo1() As System.Collections.IEnumerable
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2() As System.Collections.IEnumerable   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo4() As System.Collections.IEnumerable
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo5() As System.Collections.IEnumerable Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo6() As System.Collections.IEnumerable Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo7()
    End Sub

    '   g. Without End Function
    Iterator Function Goo3() As System.Collections.IEnumerable

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithoutAsClause_WithAddedImports()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Collections
Imports System.Collections.Generic

Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class Test
    Implements I

    '   a. Basic
    Iterator Function Goo1()
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2()   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo4
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo5() Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo6()      Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo7()
    End Sub

    '   g. Without End Function
    Iterator Function Goo3()

End Class|]", @"
Imports System
Imports System.Collections
Imports System.Collections.Generic

Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class Test
    Implements I

    '   a. Basic
    Iterator Function Goo1() As IEnumerable
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2() As IEnumerable   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo4() As IEnumerable
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo5() As IEnumerable Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo6() As IEnumerable Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo7()
    End Sub

    '   g. Without End Function
    Iterator Function Goo3() As IEnumerable

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithAsClause()
    {
        await VerifyAsync(@"[|
Interface I
    Function Goo() As System.Threading.Tasks.Task
End Interface

Class MyType
    Public Class Task
    End Class
End Class

Class MyTask
    Inherits Task
    Sub New()
        MyBase.New(Nothing)
    End Sub
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Async Function Goo1() As Integer
    End Function

    '   b. With Trailing trivia
    Async Function Goo2() As Integer   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo3 As Integer   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo4() As Integer Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo5() As Integer     Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo6() As Integer
    End Sub

    '   g. With valid Task return type
    Async Function Goo7() As Task
    End Function

    '   h. With valid Task(Of T) return type
    Async Function Goo8() As Task(Of T)
    End Function

    '   i. With valid Task(Of Integer) return type
    Async Function Goo9() As Task(Of Integer)
    End Function

    '   j. With invalid user defined Task return type
    Async Function Goo10() As MyType.Task
    End Function

    '   k. With error return type
    Async Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Async Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Async Function Goo13()    As         Integer  ' Trailing
    End Function

    '   n. With return type that inherits from Task
    Async Function Goo14() As MyTask
    End Function

    '   o. Without End Function
    Async Function GooLast() As Integer

End Class|]", @"
Interface I
    Function Goo() As System.Threading.Tasks.Task
End Interface

Class MyType
    Public Class Task
    End Class
End Class

Class MyTask
    Inherits Task
    Sub New()
        MyBase.New(Nothing)
    End Sub
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Async Function Goo1() As System.Threading.Tasks.Task(Of Integer)
    End Function

    '   b. With Trailing trivia
    Async Function Goo2() As System.Threading.Tasks.Task(Of Integer)   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo3() As System.Threading.Tasks.Task(Of Integer)   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo4() As System.Threading.Tasks.Task(Of Integer) Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo5() As System.Threading.Tasks.Task(Of Integer) Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo6() As Integer
    End Sub

    '   g. With valid Task return type
    Async Function Goo7() As Task
    End Function

    '   h. With valid Task(Of T) return type
    Async Function Goo8() As Task(Of T)
    End Function

    '   i. With valid Task(Of Integer) return type
    Async Function Goo9() As Task(Of Integer)
    End Function

    '   j. With invalid user defined Task return type
    Async Function Goo10() As System.Threading.Tasks.Task(Of MyType.Task)
    End Function

    '   k. With error return type
    Async Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Async Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Async Function Goo13() As System.Threading.Tasks.Task(Of Integer)  ' Trailing
    End Function

    '   n. With return type that inherits from Task
    Async Function Goo14() As System.Threading.Tasks.Task(Of MyTask)
    End Function

    '   o. Without End Function
    Async Function GooLast() As System.Threading.Tasks.Task(Of Integer)

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithAsClause_WithAddedImports()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks

Interface I
    Function Goo() As System.Threading.Tasks.Task
End Interface

Class MyType
    Public Class Task
    End Class
End Class

Class MyTask
    Inherits Task
    Sub New()
        MyBase.New(Nothing)
    End Sub
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Async Function Goo1() As Integer
    End Function

    '   b. With Trailing trivia
    Async Function Goo2() As Integer   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo3 As Integer   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo4() As Integer Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo5() As Integer     Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo6() As Integer
    End Sub

    '   g. With valid Task return type
    Async Function Goo7() As Task
    End Function

    '   h. With valid Task(Of T) return type
    Async Function Goo8() As Task(Of T)
    End Function

    '   i. With valid Task(Of Integer) return type
    Async Function Goo9() As Task(Of Integer)
    End Function

    '   j. With invalid user defined Task return type
    Async Function Goo10() As MyType.Task
    End Function

    '   k. With error return type
    Async Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Async Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Async Function Goo13()    As         Integer  ' Trailing
    End Function

    '   n. With return type that inherits from Task
    Async Function Goo14() As MyTask
    End Function

    '   o. Without End Function
    Async Function GooLast() As Integer

End Class|]", @"
Imports System
Imports System.Threading.Tasks

Interface I
    Function Goo() As System.Threading.Tasks.Task
End Interface

Class MyType
    Public Class Task
    End Class
End Class

Class MyTask
    Inherits Task
    Sub New()
        MyBase.New(Nothing)
    End Sub
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Async Function Goo1() As Task(Of Integer)
    End Function

    '   b. With Trailing trivia
    Async Function Goo2() As Task(Of Integer)   ' Trailing
    End Function

    '   c. Without Parenthesis
    Async Function Goo3() As Task(Of Integer)   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Async Function Goo4() As Task(Of Integer) Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Async Function Goo5() As Task(Of Integer) Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Async Sub
    Async Sub Goo6() As Integer
    End Sub

    '   g. With valid Task return type
    Async Function Goo7() As Task
    End Function

    '   h. With valid Task(Of T) return type
    Async Function Goo8() As Task(Of T)
    End Function

    '   i. With valid Task(Of Integer) return type
    Async Function Goo9() As Task(Of Integer)
    End Function

    '   j. With invalid user defined Task return type
    Async Function Goo10() As Task(Of MyType.Task)
    End Function

    '   k. With error return type
    Async Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Async Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Async Function Goo13() As Task(Of Integer)  ' Trailing
    End Function

    '   n. With return type that inherits from Task
    Async Function Goo14() As Task(Of MyTask)
    End Function

    '   o. Without End Function
    Async Function GooLast() As Task(Of Integer)

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithAsClause()
    {
        await VerifyAsync(@"[|
Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class MyType
    Public Class IEnumerable
    End Class
End Class

Class MyIEnumerable
    Implements System.Collections.IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Iterator Function Goo1() As Integer
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2() As Integer   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo3 As Integer   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo4() As Integer Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo5() As Integer     Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo6() As Integer
    End Sub

    '   g1. With valid IEnumerable return type
    Iterator Function Goo7_1() As System.Collections.IEnumerable
    End Function

    '   g2. With valid IEnumerator return type
    Iterator Function Goo7_2() As System.Collections.IEnumerator
    End Function

    '   h1. With valid IEnumerable(Of T) return type
    Iterator Function Goo8_1() As System.Collections.Generic.IEnumerable(Of T)
    End Function

    '   h2. With valid IEnumerator(Of T) return type
    Iterator Function Goo8_2() As System.Collections.Generic.IEnumerator(Of T)
    End Function

    '   i1. With valid IEnumerable(Of Integer) return type
    Iterator Function Goo9_1() As System.Collections.Generic.IEnumerable(Of Integer)
    End Function

    '   i2. With valid IEnumerator(Of Integer) return type
    Iterator Function Goo9_2() As System.Collections.Generic.IEnumerator(Of Integer)
    End Function

    '   j. With invalid user defined IEnumerable return type
    Iterator Function Goo10() As MyType.IEnumerable
    End Function

    '   k. With error return type
    Iterator Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Iterator Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Iterator Function Goo13()    As         Integer  ' Trailing
    End Function

    '   n. With return type that implements IEnumerable
    Iterator Function Goo14() As MyIEnumerable
    End Function

    '   o. Without End Function
    Iterator Function GooLast() As Integer

End Class|]", @"
Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class MyType
    Public Class IEnumerable
    End Class
End Class

Class MyIEnumerable
    Implements System.Collections.IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Iterator Function Goo1() As System.Collections.Generic.IEnumerable(Of Integer)
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2() As System.Collections.Generic.IEnumerable(Of Integer)   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo3() As System.Collections.Generic.IEnumerable(Of Integer)   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo4() As System.Collections.Generic.IEnumerable(Of Integer) Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo5() As System.Collections.Generic.IEnumerable(Of Integer) Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo6() As Integer
    End Sub

    '   g1. With valid IEnumerable return type
    Iterator Function Goo7_1() As System.Collections.IEnumerable
    End Function

    '   g2. With valid IEnumerator return type
    Iterator Function Goo7_2() As System.Collections.IEnumerator
    End Function

    '   h1. With valid IEnumerable(Of T) return type
    Iterator Function Goo8_1() As System.Collections.Generic.IEnumerable(Of T)
    End Function

    '   h2. With valid IEnumerator(Of T) return type
    Iterator Function Goo8_2() As System.Collections.Generic.IEnumerator(Of T)
    End Function

    '   i1. With valid IEnumerable(Of Integer) return type
    Iterator Function Goo9_1() As System.Collections.Generic.IEnumerable(Of Integer)
    End Function

    '   i2. With valid IEnumerator(Of Integer) return type
    Iterator Function Goo9_2() As System.Collections.Generic.IEnumerator(Of Integer)
    End Function

    '   j. With invalid user defined IEnumerable return type
    Iterator Function Goo10() As System.Collections.Generic.IEnumerable(Of MyType.IEnumerable)
    End Function

    '   k. With error return type
    Iterator Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Iterator Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Iterator Function Goo13() As System.Collections.Generic.IEnumerable(Of Integer)  ' Trailing
    End Function

    '   n. With return type that implements IEnumerable
    Iterator Function Goo14() As System.Collections.Generic.IEnumerable(Of MyIEnumerable)
    End Function

    '   o. Without End Function
    Iterator Function GooLast() As System.Collections.Generic.IEnumerable(Of Integer)

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithAsClause_WithAddedImports()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Collections.Generic

Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class MyType
    Public Class IEnumerable
    End Class
End Class

Class MyIEnumerable
    Implements System.Collections.IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Iterator Function Goo1() As Integer
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2() As Integer   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo3 As Integer   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo4() As Integer Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo5() As Integer     Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo6() As Integer
    End Sub

    '   g1. With valid IEnumerable return type
    Iterator Function Goo7_1() As System.Collections.IEnumerable
    End Function

    '   g2. With valid IEnumerator return type
    Iterator Function Goo7_2() As System.Collections.IEnumerator
    End Function

    '   h1. With valid IEnumerable(Of T) return type
    Iterator Function Goo8_1() As IEnumerable(Of T)
    End Function

    '   h2. With valid IEnumerator(Of T) return type
    Iterator Function Goo8_2() As System.Collections.Generic.IEnumerator(Of T)
    End Function

    '   i1. With valid IEnumerable(Of Integer) return type
    Iterator Function Goo9_1() As IEnumerable(Of Integer)
    End Function

    '   i2. With valid IEnumerator(Of Integer) return type
    Iterator Function Goo9_2() As System.Collections.Generic.IEnumerator(Of Integer)
    End Function

    '   j. With invalid user defined IEnumerable return type
    Iterator Function Goo10() As MyType.IEnumerable
    End Function

    '   k. With error return type
    Iterator Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Iterator Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Iterator Function Goo13()    As         Integer  ' Trailing
    End Function

    '   n. With return type that implements IEnumerable
    Iterator Function Goo14() As MyIEnumerable
    End Function

    '   o. Without End Function
    Iterator Function GooLast() As Integer

End Class|]", @"
Imports System
Imports System.Collections.Generic

Interface I
    Function Goo() As System.Collections.IEnumerable
End Interface

Class MyType
    Public Class IEnumerable
    End Class
End Class

Class MyIEnumerable
    Implements System.Collections.IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test(Of T)
    Implements I

    '   a. Basic
    Iterator Function Goo1() As IEnumerable(Of Integer)
    End Function

    '   b. With Trailing trivia
    Iterator Function Goo2() As IEnumerable(Of Integer)   ' Trailing
    End Function

    '   c. Without Parenthesis
    Iterator Function Goo3() As IEnumerable(Of Integer)   ' Trailing
    End Function

    '   d. With Implements/Handles clause
    Iterator Function Goo4() As IEnumerable(Of Integer) Implements I.Goo
    End Function

    '   e. With Implements/Handles clause with trivia
    Iterator Function Goo5() As IEnumerable(Of Integer) Implements I.Goo  ' Trailing
    End Function

    '   f. Invalid Iterator Sub
    Iterator Sub Goo6() As Integer
    End Sub

    '   g1. With valid IEnumerable return type
    Iterator Function Goo7_1() As System.Collections.IEnumerable
    End Function

    '   g2. With valid IEnumerator return type
    Iterator Function Goo7_2() As System.Collections.IEnumerator
    End Function

    '   h1. With valid IEnumerable(Of T) return type
    Iterator Function Goo8_1() As IEnumerable(Of T)
    End Function

    '   h2. With valid IEnumerator(Of T) return type
    Iterator Function Goo8_2() As System.Collections.Generic.IEnumerator(Of T)
    End Function

    '   i1. With valid IEnumerable(Of Integer) return type
    Iterator Function Goo9_1() As IEnumerable(Of Integer)
    End Function

    '   i2. With valid IEnumerator(Of Integer) return type
    Iterator Function Goo9_2() As System.Collections.Generic.IEnumerator(Of Integer)
    End Function

    '   j. With invalid user defined IEnumerable return type
    Iterator Function Goo10() As IEnumerable(Of MyType.IEnumerable)
    End Function

    '   k. With error return type
    Iterator Function Goo11() As ErrorType
    End Function

    '   l. Without a return type
    Iterator Function Goo12() As  ' Trailing
    End Function

    '   m. With trivia within AsClause
    Iterator Function Goo13() As IEnumerable(Of Integer)  ' Trailing
    End Function

    '   n. With return type that implements IEnumerable
    Iterator Function Goo14() As IEnumerable(Of MyIEnumerable)
    End Function

    '   o. Without End Function
    Iterator Function GooLast() As IEnumerable(Of Integer)

End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithAliasedReturnType()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks
Imports X = System.Threading.Tasks.Task
Imports Y = System.Threading
Class Test
    Async Function Goo() As X
    End Function
    Async Function Bar() As Y.Tasks.Task
End Class|]", @"
Imports System
Imports System.Threading.Tasks
Imports X = System.Threading.Tasks.Task
Imports Y = System.Threading
Class Test
    Async Function Goo() As X
    End Function
    Async Function Bar() As Y.Tasks.Task
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithAliasedReturnType()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Collections
Imports X = System.Collections.IEnumerable
Imports Y = System.Collections
Class Test
    Iterator Function Goo() As X
    End Function
    Iterator Function Bar() As Y.IEnumerable
End Class|]", @"
Imports System
Imports System.Collections
Imports X = System.Collections.IEnumerable
Imports Y = System.Collections
Class Test
    Iterator Function Goo() As X
    End Function
    Iterator Function Bar() As Y.IEnumerable
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithAliasedReturnType_2()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Async Function Goo() As Y      ' Trailing
    End Function
End Class|]", @"
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Async Function Goo() As Task(Of Y)      ' Trailing
    End Function
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithAliasedReturnType_2()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Iterator Function Goo() As Y      ' Trailing
    End Function
End Class|]", @"
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Iterator Function Goo() As Collections.Generic.IEnumerable(Of Y)      ' Trailing
    End Function
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncFunctionWithQualifiedNameReturnType()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks

Class Test
    Async Function Goo() As System.Int32      ' Trailing
    End Function
End Class|]", @"
Imports System
Imports System.Threading.Tasks

Class Test
    Async Function Goo() As Task(Of System.Int32)      ' Trailing
    End Function
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorFunctionWithQualifiedNameReturnType()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Collections

Class Test
    Iterator Function Goo() As System.Int32      ' Trailing
    End Function
End Class|]", @"
Imports System
Imports System.Collections

Class Test
    Iterator Function Goo() As Generic.IEnumerable(Of System.Int32)      ' Trailing
    End Function
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestAsyncLambdaFunction()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Threading.Tasks

Class Test
    Async Function Goo() As Integer
        ' Basic
        Dim a = Async Function() As Integer
                End Function

        ' With trailing trivia
        Dim b = Async Function() As Integer    ' Trailing
                End Function

        ' Single Line lambda
        Dim c = Async Function() 0

        ' Without AsClause
        Dim d = Async Function()
                End Function

        ' With valid Task return type
        Dim e = Async Function() as Task
                End Function

        ' Without End Function
        Dim last = Async Function() As Integer    ' Trailing
    End Function
End Class|]", @"
Imports System
Imports System.Threading.Tasks

Class Test
    Async Function Goo() As Task(Of Integer)
        ' Basic
        Dim a = Async Function() As Task(Of Integer)
                End Function

        ' With trailing trivia
        Dim b = Async Function() As Task(Of Integer)    ' Trailing
                End Function

        ' Single Line lambda
        Dim c = Async Function() 0

        ' Without AsClause
        Dim d = Async Function()
                End Function

        ' With valid Task return type
        Dim e = Async Function() as Task
                End Function

        ' Without End Function
        Dim last = Async Function() As Task(Of Integer)    ' Trailing
                   End Function
End Class");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
    public async Task TestIteratorLambdaFunction()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Collections.Generic

Class Test
    Iterator Function Goo() As Integer
        ' Basic
        Dim a = Iterator Function() As Integer
                End Function

        ' With trailing trivia
        Dim b = Iterator Function() As Integer    ' Trailing
                End Function

        ' Single Line lambda
        Dim c = Iterator Function() 0

        ' Without AsClause
        Dim d = Iterator Function()
                End Function

        ' With valid IEnumerable return type
        Dim e = Iterator Function() as System.Collections.IEnumerable
                End Function

        ' Without End Function
        Dim last = Iterator Function() As Integer    ' Trailing
    End Function
End Class|]", @"
Imports System
Imports System.Collections.Generic

Class Test
    Iterator Function Goo() As IEnumerable(Of Integer)
        ' Basic
        Dim a = Iterator Function() As IEnumerable(Of Integer)
                End Function

        ' With trailing trivia
        Dim b = Iterator Function() As IEnumerable(Of Integer)    ' Trailing
                End Function

        ' Single Line lambda
        Dim c = Iterator Function() 0

        ' Without AsClause
        Dim d = Iterator Function()
                End Function

        ' With valid IEnumerable return type
        Dim e = Iterator Function() as System.Collections.IEnumerable
                End Function

        ' Without End Function
        Dim last = Iterator Function() As IEnumerable(Of Integer)    ' Trailing
                   End Function
End Class");
    }

    [Fact]
    public async Task TestNoParenthesesForArgument()
    {
        await VerifyAsync(@"[|
Imports System
Imports System.Collections.Generic

Class Test
    Private Function Goo() As Integer
        Return 1
    End Function

    Private Sub Caller(i As Integer)
        Caller(Goo)
    End Sub
End Class|]", @"
Imports System
Imports System.Collections.Generic

Class Test
    Private Function Goo() As Integer
        Return 1
    End Function

    Private Sub Caller(i As Integer)
        Caller(Goo)
    End Sub
End Class");
    }

    [Fact]
    public async Task TestNoParenthesesForNameOf()
    {
        await VerifyAsync(@"[|
Module M
    Sub Main()
        Dim s = NameOf(Main)
    End Sub
End Module|]", @"
Module M
    Sub Main()
        Dim s = NameOf(Main)
    End Sub
End Module");
    }

    [Fact]
    public async Task OptionExplicitOn()
    {
        await VerifyAsync(@"[|Option Explicit|]", @"Option Explicit On
");
    }

    [Fact]
    public async Task OptionInferOn()
    {
        await VerifyAsync(@"[|Option Infer|]", @"Option Infer On
");
    }

    [Fact]
    public async Task OptionStrictOn()
    {
        await VerifyAsync(@"[|Option Strict|]", @"Option Strict On
");
    }

    private static string CreateMethod(string body)
    {
        return @"Imports System
Class C
    Public Sub Method()" + body + @"
    End Sub
End Class";
    }

    private static async Task VerifyAsync(string codeWithMarker, string expectedResult)
    {
        MarkupTestFile.GetSpans(codeWithMarker, out var codeWithoutMarker, out var textSpans);

        var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
        var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name is PredefinedCodeCleanupProviderNames.AddMissingTokens or PredefinedCodeCleanupProviderNames.Format or PredefinedCodeCleanupProviderNames.Simplification);

        var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], await document.GetCodeCleanupOptionsAsync(CancellationToken.None), codeCleanups);

        Assert.Equal(expectedResult, (await cleanDocument.GetSyntaxRootAsync()).ToFullString());
    }

    private static Document CreateDocument(string code, string language)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

        return project.AddMetadataReference(NetFramework.mscorlib)
                      .AddDocument("Document", SourceText.From(code));
    }
}
