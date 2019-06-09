// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    [UseExportProvider]
    public class AddMissingTokensTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MultipleLineIfStatementThen()
        {
            var code = @"[|
        If True
            Dim a = 1
        End If|]";

            var expected = @"
        If True Then
            Dim a = 1
        End If";

            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TypeArgumentOf()
        {
            var code = @"[|
        Dim a As List(Integer)|]";

            var expected = @"
        Dim a As List(Of Integer)";

            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TypeParameterOf()
        {
            var code = @"[|Class A(T)
End Class|]";

            var expected = @"Class A(Of T)
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodDeclaration()
        {
            var code = @"Class A
    [|Sub Test
    End Sub|]
End Class";

            var expected = @"Class A
    Sub Test()
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544318")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_TypeArgParens()
        {
            var code = @"[|Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

Module Program
    Sub Main(args As String())
        [|Dim q = New List(Of Thread

    |]End Sub|]";

            var expected = @"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Thread

    End Sub";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_Sub()
        {
            var code = @"Class A
    Sub Test
        [|Test|]
    End Sub
End Class";

            var expected = @"Class A
    Sub Test
        Test()
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_Function()
        {
            var code = @"Class A
    Function Test() As Integer
        [|Test|]
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Test()
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task IdentifierMethod_Return()
        {
            var code = @"Class A
    Function Test() As Integer
        Return [|Test2|]
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Return Test2()
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task IdentifierMethod_Assign()
        {
            var code = @"Class A
    Function Test() As Integer
        Dim a = [|Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Dim a = Test2()
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task IdentifierMethod_DotName_DontAdd()
        {
            var code = @"Class A
    Function Test() As Integer
        Dim a = [|Me.Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Dim a = Me.Test2
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_DotName()
        {
            var code = @"Class A
    Function Test() As Integer
        [|Me.Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Me.Test2()
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_Generic()
        {
            var code = @"Class A
    Function Test() As Integer
        [|Me.Test2(Of Integer)|]
        Return 1
    End Function

    Function Test2(Of T) As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Me.Test2(Of Integer)()
        Return 1
    End Function

    Function Test2(Of T) As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_Call()
        {
            var code = @"Class A
    Function Test() As Integer
        Call [|Me.Test2|]
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Call Me.Test2()
        Return 1
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task EventHandler_AddressOf1()
        {
            var code = @"Class A
    Sub EventMethod()
        [|AddHandler TestEvent, AddressOf EventMethod|]
    End Sub

    Public Event TestEvent()
End Class";

            var expected = @"Class A
    Sub EventMethod()
        AddHandler TestEvent, AddressOf EventMethod
    End Sub

    Public Event TestEvent()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task EventHandler_AddressOf2()
        {
            var code = @"Class A
    Sub EventMethod()
        [|RemoveHandler TestEvent, AddressOf EventMethod|]
    End Sub

    Public Event TestEvent()
End Class";

            var expected = @"Class A
    Sub EventMethod()
        RemoveHandler TestEvent, AddressOf EventMethod
    End Sub

    Public Event TestEvent()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task Delegate_AddressOf()
        {
            var code = @"Class A
    Sub Method()
        [|Dim a As Action = New Action(AddressOf Method)|]
    End Sub
End Class";

            var expected = @"Class A
    Sub Method()
        Dim a As Action = New Action(AddressOf Method)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task EventDeclaration()
        {
            var code = @"Class A
    Sub EventMethod()
        [|RaiseEvent TestEvent|]
    End Sub

    Public Event TestEvent()
End Class";

            var expected = @"Class A
    Sub EventMethod()
        RaiseEvent TestEvent()
    End Sub

    Public Event TestEvent()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task RaiseEvent()
        {
            var code = @"Class A
    Sub EventMethod()
        RaiseEvent TestEvent
    End Sub

    [|Public Event TestEvent|]
End Class";

            var expected = @"Class A
    Sub EventMethod()
        RaiseEvent TestEvent
    End Sub

    Public Event TestEvent()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DelegateInvocation()
        {
            var code = @"
        Dim a As Action
        [|a|]";

            var expected = @"
        Dim a As Action
        a()";

            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task Attribute()
        {
            var code = @"[|<Obsolete>
Class C(Of T)
    Sub Main
        Dim a = {1, 2, 3}
    End Sub
End Class|]";

            var expected = @"<Obsolete>
Class C(Of T)
    Sub Main()
        Dim a = {1, 2, 3}
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ObjectCreation()
        {
            var code = @"[|Module Program
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
End Class|]";

            var expected = @"Module Program
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
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task Constructor()
        {
            var code = @"[|Class C
    Sub New
    End Sub
End Class|]";

            var expected = @"Class C
    Sub New()
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DeclareStatement()
        {
            var code = @"[|Class C
    Declare Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA"" 
End Class|]";

            var expected = @"Class C
    Declare Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA"" ()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DelegateStatement()
        {
            var code = @"[|Class C
    Delegate Sub Test
End Class|]";

            var expected = @"Class C
    Delegate Sub Test()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodStatementWithComment()
        {
            var code = @"[|Class C
    [|Sub Test ' test
    End Sub|]
End Class|]";

            var expected = @"Class C
    Sub Test() ' test
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MultipleLineIfStatementThenWithComment()
        {
            var code = @"[|
        If True ' test
            Dim a = 1
        End If|]";

            var expected = @"
        If True Then ' test
            Dim a = 1
        End If";

            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TypeArgumentOf_Comment_DontAdd()
        {
            var code = @"[|
        Dim a As List( ' test
                      Integer)|]";

            var expected = @"
        Dim a As List( ' test
                      Integer)";

            // parser doesn't recognize the broken list as Type Argument List
            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TypeParameterOf_Comment()
        {
            var code = @"[|Class A( ' test
                                   T)
End Class|]";

            var expected = @"Class A(Of ' test
                                   T)
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task MethodInvocation_Function_Comment()
        {
            var code = @"Class A
    Function Test() As Integer
        [|Test ' test|]
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Test() ' test
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task IdentifierMethod_Return_Comment()
        {
            var code = @"Class A
    Function Test() As Integer
        Return [|Test2 ' test|]
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            var expected = @"Class A
    Function Test() As Integer
        Return Test2() ' test
    End Function

    Function Test2() As Integer
        Return 1
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ImplementsClause()
        {
            var code = @"Class Program
    Implements I
    [|Public Sub Method() Implements I.Method
    End Sub|]
End Class

Interface I
    Sub Method()
End Interface";

            var expected = @"Class Program
    Implements I
    Public Sub Method() Implements I.Method
    End Sub
End Class

Interface I
    Sub Method()
End Interface";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task OperatorStatement()
        {
            var code = @"[|Public Structure abc
    Public Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
        Dim r As New abc
        ' Insert code to calculate And of x and y.
        Return r
    End Operator
End Structure|]";

            var expected = @"Public Structure abc
    Public Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
        Dim r As New abc
        ' Insert code to calculate And of x and y.
        Return r
    End Operator
End Structure";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task PropertyAndAccessorStatement()
        {
            var code = @"[|Class Class1
    Private propertyValue As String
    Public Property prop1() As String
        Get
            Return propertyValue
        End Get
        Set(ByVal value As String)
            propertyValue = value
        End Set
    End Property
End Class|]";

            var expected = @"Class Class1
    Private propertyValue As String
    Public Property prop1() As String
        Get
            Return propertyValue
        End Get
        Set(ByVal value As String)
            propertyValue = value
        End Set
    End Property
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task LambdaExpression()
        {
            var code = @"[|Class Class1
    Dim f as Action = Sub()
                      End Sub
End Class|]";

            var expected = @"Class Class1
    Dim f as Action = Sub()
                      End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544225")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task StructuredTrivia_Expression_DontCrash()
        {
            var code = @"[|#Const Goo1 = 1
#Const Goo2 = 2
#If Goo1 Then
#ElseIf Goo2 Then
#Else
#End If|]";
            var expected = @"#Const Goo1 = 1
#Const Goo2 = 2
#If Goo1 Then
#ElseIf Goo2 Then
#Else
#End If";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544169")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task EventStatement_AsClause()
        {
            var code = @"[|Imports System.ComponentModel
Class Goo
    Public Event PropertyChanged As PropertyChangedEventHandler
End Class|]";
            var expected = @"Imports System.ComponentModel
Class Goo
    Public Event PropertyChanged As PropertyChangedEventHandler
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544167")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task InvocationExpression_NoParenthesesForPredefinedCastExpression()
        {
            var code = @"[|Class Program
    Sub Main(args As String())
        CInt(5)
    End Sub
End Class|]";
            var expected = @"Class Program
    Sub Main(args As String())
        CInt(5)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544178")]
        [WorkItem(544317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544317")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ObjectCreationExpression()
        {
            var code = @"[|Class C
    Function F() As C
        Return New C
    End Function
End Class|]";
            var expected = @"Class C
    Function F() As C
        Return New C
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544317")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ObjectCreationExpression_Initializer()
        {
            var code = @"[|Public Class SomeClass
    Public goo As Integer

    Sub SomeSub()
        [|Dim c = New SomeClass With {.goo = 23}|]
    End Sub
End Class|]";
            var expected = @"Public Class SomeClass
    Public goo As Integer

    Sub SomeSub()
        Dim c = New SomeClass With {.goo = 23}
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544178")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ObjectCreationExpression_GenericName()
        {
            var code = @"[|Imports System

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Integer
    End Sub
End Module|]";

            var expected = @"Imports System

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Integer
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544178")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ObjectCreationExpression_AsNewClause()
        {
            var code = @"[|Class C
    Dim a As New C
End Class|]";
            var expected = @"Class C
    Dim a As New C
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544301")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ContinueStatement_While()
        {
            var code = @"Module M
    Sub S()
        [|While True
            Continue
        End While|]
    End Sub
End Module";
            var expected = @"Module M
    Sub S()
        While True
            Continue While
        End While
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544301")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ContinueStatement_For()
        {
            var code = @"Module M
    Sub S()
        [|For i = 1 to 10
            Continue
        Next|]
    End Sub
End Module";
            var expected = @"Module M
    Sub S()
        For i = 1 to 10
            Continue For
        Next
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544380")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task IfDirective()
        {
            var code = @"[|#If VBC_VER >= 9.0

Class C
End Class|]";
            var expected = @"#If VBC_VER >= 9.0 Then

Class C
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544386")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task NamedFieldInitializer()
        {
            var code = @"[|Class S
    Public Sub Goo()
    End Sub
    Property X
    Sub test()
        Dim x = New S With {.X = 0,.Goo}
    End Sub
End Class|]";
            var expected = @"Class S
    Public Sub Goo()
    End Sub
    Property X
    Sub test()
        Dim x = New S With {.X = 0, .Goo}
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544526")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DontCrash_ImplementsStatement()
        {
            var code = @"[|Class C
    Sub Main() 
        Implements IDisposable.Dispose
    End Sub
End Class|]";
            var expected = @"Class C
    Sub Main()
        Implements IDisposable.Dispose
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(544525, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544525")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task AccessorStatement_AddRemoveHandler_RaiseEvent()
        {
            var code = @"[|Class C
    Public Custom Event E1 As Action
        AddHandler
        End AddHandler
        RemoveHandler
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
End Class|]";
            var expected = @"Class C
    Public Custom Event E1 As Action
        AddHandler()
        End AddHandler
        RemoveHandler()
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(545176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545176")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task CallStatement_Lambda()
        {
            var code = @"[|Module Program
    Sub Main()
        Call Sub() Console.WriteLine(1)
    End Sub
End Module|]";
            var expected = @"Module Program
    Sub Main()
        Call Sub() Console.WriteLine(1)
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(545256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545256")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task HandlesClauseItem_DontAddParentheses()
        {
            var code = @"[|Structure s1
    Sub Goo() Handles Me.Goo
 
    End Sub
End Structure|]";
            var expected = @"Structure s1
    Sub Goo() Handles Me.Goo

    End Sub
End Structure";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(545380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545380")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DontAddParenthesesInForEachControlVariable()
        {
            var code = @"[|Module Module1
    Sub Main()
        For Each goo in {} 
    End Sub
 
    Sub Goo()
    End Sub
End Module|]";
            var expected = @"Module Module1
    Sub Main()
        For Each goo in {}
    End Sub

    Sub Goo()
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(545380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545380")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DontAddParenthesesInForControlVariable()
        {
            var code = @"[|Module Module1
    Sub Main()
        For goo to 
    End Sub
 
    Sub Goo()
    End Sub
End Module|]";
            var expected = @"Module Module1
    Sub Main()
        For goo to 
    End Sub

    Sub Goo()
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(545483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545483")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task DontAddParenthesesForMissingName()
        {
            var code = @"[|Class C
    Public Overrides Function|]";
            var expected = @"Class C
    Public Overrides Function";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(545483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545483")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task CombinedDelegates()
        {
            var code = @"[|Imports System
Class A
    Public Shared Operator +(x As A, y As A) As Action
    End Operator
    Shared Sub Main()
        Dim x As New A
        Call x + x
    End Sub
End Class|]";
            var expected = @"Imports System
Class A
    Public Shared Operator +(x As A, y As A) As Action
    End Operator
    Shared Sub Main()
        Dim x As New A
        Call x + x
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(546581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546581")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ThenOmittedWithSurroundingErrors()
        {
            var code = @"[|
        If True OrElse|]";
            var expected = @"
        If True OrElse";

            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [WorkItem(546581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546581")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ThenOmittedWithSurroundingErrors1()
        {
            var code = @"[|
        If True|]";
            var expected = @"
        If True Then";

            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [WorkItem(546797, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546797")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ParenthesisWithLineContinuation()
        {
            var code = @"[|
            System.Diagnostics.Debug.Assert _ (True)|]";
            var expected = @"
        System.Diagnostics.Debug.Assert _ (True)";
            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [WorkItem(546806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546806")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ThenWithLineContinuation()
        {
            var code = @"[|
#If Condition _ Then
            ' blah
#End If|]";
            var expected = @"
#If Condition _ Then
        ' blah
#End If";
            await VerifyAsync(CreateMethod(code), CreateMethod(expected));
        }

        [Fact]
        [WorkItem(531278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531278")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task ThenInIfDirective()
        {
            var code = @"#Const ccConst = 0
[|#If ccConst
#End If|]
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
    End Sub
End Module";
            var expected = @"#Const ccConst = 0
#If ccConst Then
#End If
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
    End Sub
End Module";
            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(607792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607792")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task CaseKeywordInSelectStatement()
        {
            var code = @"
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
End Module";

            var expected = @"
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
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(530789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530789")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task Bug530789()
        {
            var code = @"Imports System
Module Program
    Sub Main()
        [|If True Then Console.WriteLine else If False Then Console.WriteLine else Console.writeline|]
    End Sub
End Module";

            var expected = @"Imports System
Module Program
    Sub Main()
        If True Then Console.WriteLine() else If False Then Console.WriteLine() else Console.writeline()
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(530039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530039")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestArraySyntax()
        {
            var code = @"[|Module TestMod
Sub Main()
Dim y As Object
Dim x As cls2(Of y.gettype())
End Sub
End Module|]";

            var expected = @"Module TestMod
    Sub Main()
        Dim y As Object
        Dim x As cls2(Of y.gettype())
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithoutAsClause()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithoutAsClause_WithAddedImports()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithoutAsClause()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithoutAsClause_WithAddedImports()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithAsClause()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithAsClause_WithAddedImports()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithAsClause()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithAsClause_WithAddedImports()
        {
            var code = @"[|
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

End Class|]";

            var expected = @"
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

End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithAliasedReturnType()
        {
            var code = @"[|
Imports System
Imports System.Threading.Tasks
Imports X = System.Threading.Tasks.Task
Imports Y = System.Threading
Class Test
    Async Function Goo() As X
    End Function
    Async Function Bar() As Y.Tasks.Task
End Class|]";

            var expected = @"
Imports System
Imports System.Threading.Tasks
Imports X = System.Threading.Tasks.Task
Imports Y = System.Threading
Class Test
    Async Function Goo() As X
    End Function
    Async Function Bar() As Y.Tasks.Task
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithAliasedReturnType()
        {
            var code = @"[|
Imports System
Imports System.Collections
Imports X = System.Collections.IEnumerable
Imports Y = System.Collections
Class Test
    Iterator Function Goo() As X
    End Function
    Iterator Function Bar() As Y.IEnumerable
End Class|]";

            var expected = @"
Imports System
Imports System.Collections
Imports X = System.Collections.IEnumerable
Imports Y = System.Collections
Class Test
    Iterator Function Goo() As X
    End Function
    Iterator Function Bar() As Y.IEnumerable
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithAliasedReturnType_2()
        {
            var code = @"[|
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Async Function Goo() As Y      ' Trailing
    End Function
End Class|]";

            var expected = @"
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Async Function Goo() As Task(Of Y)      ' Trailing
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithAliasedReturnType_2()
        {
            var code = @"[|
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Iterator Function Goo() As Y      ' Trailing
    End Function
End Class|]";

            var expected = @"
Imports System
Imports System.Threading.Tasks
Imports Y = System.Int32

Class Test
    Iterator Function Goo() As Collections.Generic.IEnumerable(Of Y)      ' Trailing
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncFunctionWithQualifiedNameReturnType()
        {
            var code = @"[|
Imports System
Imports System.Threading.Tasks

Class Test
    Async Function Goo() As System.Int32      ' Trailing
    End Function
End Class|]";

            var expected = @"
Imports System
Imports System.Threading.Tasks

Class Test
    Async Function Goo() As Task(Of System.Int32)      ' Trailing
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorFunctionWithQualifiedNameReturnType()
        {
            var code = @"[|
Imports System
Imports System.Collections

Class Test
    Iterator Function Goo() As System.Int32      ' Trailing
    End Function
End Class|]";

            var expected = @"
Imports System
Imports System.Collections

Class Test
    Iterator Function Goo() As Generic.IEnumerable(Of System.Int32)      ' Trailing
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestAsyncLambdaFunction()
        {
            var code = @"[|
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
End Class|]";

            var expected = @"
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
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [WorkItem(602932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602932")]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestIteratorLambdaFunction()
        {
            var code = @"[|
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
End Class|]";

            var expected = @"
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
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestNoParenthesesForArgument()
        {
            // making roslyn behavior same as dev12
            // also, this is one of most expensive one to check whether
            // parentheses needs to be inserted or not.

            var code = @"[|
Imports System
Imports System.Collections.Generic

Class Test
    Private Function Goo() As Integer
        Return 1
    End Function

    Private Sub Caller(i As Integer)
        Caller(Goo)
    End Sub
End Class|]";

            var expected = @"
Imports System
Imports System.Collections.Generic

Class Test
    Private Function Goo() As Integer
        Return 1
    End Function

    Private Sub Caller(i As Integer)
        Caller(Goo)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task TestNoParenthesesForNameOf()
        {
            var code = @"[|
Module M
    Sub Main()
        Dim s = NameOf(Main)
    End Sub
End Module|]";

            var expected = @"
Module M
    Sub Main()
        Dim s = NameOf(Main)
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task OptionExplicitOn()
        {
            var code = @"[|Option Explicit|]";
            var expected = @"Option Explicit On
";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task OptionInferOn()
        {
            var code = @"[|Option Infer|]";
            var expected = @"Option Infer On
";

            await VerifyAsync(code, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AddMissingTokens)]
        public async Task OptionStrictOn()
        {
            var code = @"[|Option Strict|]";
            var expected = @"Option Strict On
";

            await VerifyAsync(code, expected);
        }

        private string CreateMethod(string body)
        {
            return @"Imports System
Class C
    Public Sub Method()" + body + @"
    End Sub
End Class";
        }

        private async Task VerifyAsync(string codeWithMarker, string expectedResult)
        {
            MarkupTestFile.GetSpans(codeWithMarker,
                out var codeWithoutMarker, out ImmutableArray<TextSpan> textSpans);

            var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
            var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name == PredefinedCodeCleanupProviderNames.AddMissingTokens || p.Name == PredefinedCodeCleanupProviderNames.Format || p.Name == PredefinedCodeCleanupProviderNames.Simplification);

            var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], codeCleanups);

            Assert.Equal(expectedResult, (await cleanDocument.GetSyntaxRootAsync()).ToFullString());
        }

        private static Document CreateDocument(string code, string language)
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

            return project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
                          .AddDocument("Document", SourceText.From(code));
        }
    }
}
