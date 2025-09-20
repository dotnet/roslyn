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
[Trait(Traits.Feature, Traits.Features.NormalizeModifiersOrOperators)]
public sealed class NormalizeModifiersOrOperatorsTests
{
    [Fact]
    public Task PartialMethod()
        => VerifyAsync("""
            [|Class A
                Private Partial Sub()
                End Sub
            End Class|]
            """, """
            Class A
                Partial Private Sub()
                End Sub
            End Class
            """);

    [Fact]
    public Task PartialClass()
        => VerifyAsync("""
            [|Public Partial Class A
            End Class|]
            """, """
            Partial Public Class A
            End Class
            """);

    [Fact]
    public Task DefaultProperty()
        => VerifyAsync("""
            [|Class Class1
                Public Default Property prop1(i As Integer) As Integer
                    Get
                        Return i
                    End Get
                    Set(ByVal value As Integer)
                    End Set
                End Property
            End Class|]
            """, """
            Class Class1
                Default Public Property prop1(i As Integer) As Integer
                    Get
                        Return i
                    End Get
                    Set(ByVal value As Integer)
                    End Set
                End Property
            End Class
            """);

    [Fact]
    public Task Accessors()
        => VerifyAsync("""
            [|Public Module M
            End Module

            NotInheritable Friend Class C
                MustInherit Protected Friend Class N
                    Overridable Public  Sub Test()
                    End Sub

                    MustOverride Protected  Sub Test2()

                    Shared Private  Sub Test3()
                    End Sub
                End Class

                Public Class O
                    Inherits N

                    Shadows Public Sub Test()
                    End Sub

                    Overrides Protected Sub Test2()
                    End Sub
                End Class
            End Class|]
            """, """
            Public Module M
            End Module

            Friend NotInheritable Class C
                Protected Friend MustInherit Class N
                    Public Overridable Sub Test()
                    End Sub

                    Protected MustOverride Sub Test2()

                    Private Shared Sub Test3()
                    End Sub
                End Class

                Public Class O
                    Inherits N

                    Public Shadows Sub Test()
                    End Sub

                    Protected Overrides Sub Test2()
                    End Sub
                End Class
            End Class
            """);

    [Fact]
    public Task Structure()
        => VerifyAsync("""
            [|Public Partial Structure S
            End Structure|]
            """, """
            Partial Public Structure S
            End Structure
            """);

    [Fact]
    public Task Interface()
        => VerifyAsync("""
            [|Public Interface O
                Public Interface S
                End Interface
            End Interface

            Public Interface O2
                Inherits O

                Shadows Public Interface S
                End Interface
            End Interface|]
            """, """
            Public Interface O
                Public Interface S
                End Interface
            End Interface

            Public Interface O2
                Inherits O

                Public Shadows Interface S
                End Interface
            End Interface
            """);

    [Fact]
    public Task Class()
        => VerifyAsync("""
            [|MustInherit Public  Class C
            End Class|]
            """, """
            Public MustInherit Class C
            End Class
            """);

    [Fact]
    public Task Enum()
        => VerifyAsync("""
            [|Public Class O
                Public Enum S
                    None
                End Enum
            End Class

            Public Class O2
                Inherits O

                Shadows Public  Enum S
                    None
                End Enum
            End Class|]
            """, """
            Public Class O
                Public Enum S
                    None
                End Enum
            End Class

            Public Class O2
                Inherits O

                Public Shadows Enum S
                    None
                End Enum
            End Class
            """);

    [Fact]
    public Task Method()
        => VerifyAsync("""
            [|Public Class O
                Overridable Protected Function Test() As Integer
                    Return 0
                End Function
            End Class|]
            """, """
            Public Class O
                Protected Overridable Function Test() As Integer
                    Return 0
                End Function
            End Class
            """);

    [Fact]
    public Task Declare()
        => VerifyAsync("""
            [|Class C
                Overloads Public  Declare Function getUserName Lib "advapi32.dll" Alias "GetUserNameA" (ByVal lpBuffer As String, ByRef nSize As Integer) As Integer
            End Class|]
            """, """
            Class C
                Public Overloads Declare Function getUserName Lib "advapi32.dll" Alias "GetUserNameA" (ByVal lpBuffer As String, ByRef nSize As Integer) As Integer
            End Class
            """);

    [Fact]
    public Task Delegate()
        => VerifyAsync("""
            [|Public Class O
                Public Delegate Function S() As Integer
            End Class

            Public Class O2
                Inherits O

                Shadows Public  Delegate Function S() As Integer
            End Class|]
            """, """
            Public Class O
                Public Delegate Function S() As Integer
            End Class

            Public Class O2
                Inherits O

                Public Shadows Delegate Function S() As Integer
            End Class
            """);

    [Fact]
    public Task Event()
        => VerifyAsync("""
            [|Public Class O
                Shared Public  Event Test As System.EventHandler
            End Class|]
            """, """
            Public Class O
                Public Shared Event Test As System.EventHandler
            End Class
            """);

    [Fact]
    public Task Operator()
        => VerifyAsync("""
            [|Public Structure abc
                Shared Overloads Public  Operator And(ByVal x As abc, ByVal y As abc) As abc
                End Operator
            End Structure|]
            """, """
            Public Structure abc
                Public Overloads Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
                End Operator
            End Structure
            """);

    [Fact]
    public Task Property()
        => VerifyAsync("""
            [|Class Class1
               Overridable  Public  Property prop1 As Integer
            End Class|]
            """, """
            Class Class1
                Public Overridable Property prop1 As Integer
            End Class
            """);

    [Fact]
    public Task Accessor()
        => VerifyAsync("""
            [|Class Class1
                Public Property prop1 As Integer
                    Private Get
                        Return 0
                    End Get
                    Set(value As Integer)

                    End Set
                End Property
            End Class|]
            """, """
            Class Class1
                Public Property prop1 As Integer
                    Private Get
                        Return 0
                    End Get
                    Set(value As Integer)

                    End Set
                End Property
            End Class
            """);

    [Fact]
    public Task IncompleteMember()
        => VerifyAsync("""
            [|Class Program
                Shared Private Dim
            End Class|]
            """, """
            Class Program
                Shared Private Dim
            End Class
            """);

    [Fact]
    public Task Field()
        => VerifyAsync("""
            [|Class Program
                Shared ReadOnly Private Dim f = 1
            End Class|]
            """, """
            Class Program
                Private Shared ReadOnly f = 1
            End Class
            """);

    [Fact]
    public Task NotOverridable_Overridable_Overrides()
        => VerifyAsync("""
            [|Public Class Program
                Class N
                    Inherits Program

                    Overrides Public   NotOverridable Sub test()
                        MyBase.test()
                    End Sub
                End Class

                Overridable Public  Sub test()
                End Sub
            End Class|]
            """, """
            Public Class Program
                Class N
                    Inherits Program

                    Public NotOverridable Overrides Sub test()
                        MyBase.test()
                    End Sub
                End Class

                Public Overridable Sub test()
                End Sub
            End Class
            """);

    [Fact]
    public Task MustOverride_MustInherit()
        => VerifyAsync("""
            [|MustInherit Public Class Program
                MustOverride Public Sub test()
            End Class|]
            """, """
            Public MustInherit Class Program
                Public MustOverride Sub test()
            End Class
            """);

    [Fact]
    public Task Overloads()
        => VerifyAsync("""
            [|Public MustInherit Class Program
               Overloads Public  Sub test()
                End Sub

                Overloads Public  Sub test(i As Integer)
                End Sub
            End Class|]
            """, """
            Public MustInherit Class Program
                Public Overloads Sub test()
                End Sub

                Public Overloads Sub test(i As Integer)
                End Sub
            End Class
            """);

    [Fact]
    public Task NotInheritable()
        => VerifyAsync("""
            [|NotInheritable Public Class Program
            End Class|]
            """, """
            Public NotInheritable Class Program
            End Class
            """);

    [Fact]
    public Task Shared_Shadow_ReadOnly_Const()
        => VerifyAsync("""
            [|Class C
                Class N
                    Public  Sub Test()
                    End Sub

                    Const Private  Dim c As Integer = 2
                    Shared ReadOnly Private Dim f = 1
                End Class

                Public Class O
                    Inherits N

                    Shadows Public Sub Test()
                    End Sub
                End Class
            End Class|]
            """, """
            Class C
                Class N
                    Public Sub Test()
                    End Sub

                    Private Const c As Integer = 2
                    Private Shared ReadOnly f = 1
                End Class

                Public Class O
                    Inherits N

                    Public Shadows Sub Test()
                    End Sub
                End Class
            End Class
            """);

    [Fact]
    public Task WriteOnly()
        => VerifyAsync("""
            [|Class C
                WriteOnly Public  Property Test
                    Set(value)
                    End Set
                End Property
            End Class|]
            """, """
            Class C
                Public WriteOnly Property Test
                    Set(value)
                    End Set
                End Property
            End Class
            """);

    [Fact]
    public Task WithEvent_Custom_Dim()
        => VerifyAsync("""
            [|Imports System

            Public Class A
                 Public Custom Event MyEvent As EventHandler
                    AddHandler(value As EventHandler)
                    End AddHandler

                    RemoveHandler(value As EventHandler)
                    End RemoveHandler

                    RaiseEvent(sender As Object, e As EventArgs)
                    End RaiseEvent
                End Event
            End Class

            Class B
                WithEvents Dim EventSource As A
                Public Sub EventHandler(s As Object, a As EventArgs) Handles EventSource.MyEvent
                End Sub
            End Class|]
            """, """
            Imports System

            Public Class A
                Public Custom Event MyEvent As EventHandler
                    AddHandler(value As EventHandler)
                    End AddHandler

                    RemoveHandler(value As EventHandler)
                    End RemoveHandler

                    RaiseEvent(sender As Object, e As EventArgs)
                    End RaiseEvent
                End Event
            End Class

            Class B
                Dim WithEvents EventSource As A
                Public Sub EventHandler(s As Object, a As EventArgs) Handles EventSource.MyEvent
                End Sub
            End Class
            """);

    [Fact]
    public Task Widening_Narrowing()
        => VerifyAsync("""
            [|Public Structure digit
            Widening  Shared  Public Operator CType(ByVal d As digit) As Byte
                    Return 0
                End Operator
                 Narrowing Public Shared  Operator CType(ByVal b As Byte) As digit
                    Return Nothing
                End Operator
            End Structure|]
            """, """
            Public Structure digit
                Public Shared Widening Operator CType(ByVal d As digit) As Byte
                    Return 0
                End Operator
                Public Shared Narrowing Operator CType(ByVal b As Byte) As digit
                    Return Nothing
                End Operator
            End Structure
            """);

    [Fact]
    public Task Static_Const_Dim()
        => VerifyAsync("""
            [|Class A
                Sub Method()
                    Dim Static a As Integer = 1
                    Const a2 As Integer = 2
                End Sub
            End Class|]
            """, """
            Class A
                Sub Method()
                    Static Dim a As Integer = 1
                    Const a2 As Integer = 2
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
    public Task RemoveByVal1()
        => VerifyAsync("""
            [|Class A
                Sub Method(ByVal t As String)
                End Sub
            End Class|]
            """, """
            Class A
                Sub Method(ByVal t As String)
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
    public Task RemoveByVal2()
        => VerifyAsync("""
            [|Class A
                Sub Method(ByVal t As String, ByRef t1 As String)
                End Sub
            End Class|]
            """, """
            Class A
                Sub Method(ByVal t As String, ByRef t1 As String)
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
    public Task RemoveByVal_LineContinuation()
        => VerifyAsync("""
            [|Class A
                Sub Method( _
                    ByVal _
                          _
                        t As String, ByRef t1 As String)
                End Sub
            End Class|]
            """, """
            Class A
                Sub Method( _
                    ByVal _
                          _
                        t As String, ByRef t1 As String)
                End Sub
            End Class
            """);

    [Fact]
    public Task RemoveDim()
        => VerifyAsync("""
            [|Class A
                Dim  Shared Private a As Integer = 1
            End Class|]
            """, """
            Class A
                Private Shared a As Integer = 1
            End Class
            """);

    [Fact]
    public Task RemoveDim_LineContinuation()
        => VerifyAsync("""
            [|Class A
                Dim _
                    Shared _
                    Private _
                        a As Integer = 1
            End Class|]
            """, """
            Class A
                Private _
                    Shared _
                           _
                        a As Integer = 1
            End Class
            """);

    [Fact]
    public Task LessThanGreaterThan()
        => VerifyAsync("""
            [|Class A
                Sub Test()
                    If 1 >< 2 Then
                    End If
                End Sub
            End Class|]
            """, """
            Class A
                Sub Test()
                    If 1 <> 2 Then
                    End If
                End Sub
            End Class
            """);

    [Fact]
    public Task GreaterThanEquals()
        => VerifyAsync("""
            [|Class A
                Sub Test()
                    If 1 => 2 Then
                    End If
                End Sub
            End Class|]
            """, """
            Class A
                Sub Test()
                    If 1 >= 2 Then
                    End If
                End Sub
            End Class
            """);

    [Fact]
    public Task LessThanEquals()
        => VerifyAsync("""
            [|Class A
                Sub Test()
                    If 1 =< 2 Then
                    End If
                End Sub
            End Class|]
            """, """
            Class A
                Sub Test()
                    If 1 <= 2 Then
                    End If
                End Sub
            End Class
            """);

    [Fact]
    public Task LessThanEquals_LineContinuation()
        => VerifyAsync("""
            [|Class A
                Sub Test()
                    If 1 _ 
                        = _ 
                        < _
                            2 Then
                    End If
                End Sub
            End Class|]
            """, """
            Class A
                Sub Test()
                    If 1 _
                        <= _
                            2 Then
                    End If
                End Sub
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544300")]
    public Task NormalizedOperator_StructuredTrivia()
        => VerifyAsync(@"[|#If VBC_VER => 9.0|]", @"#If VBC_VER >= 9.0");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
    public Task DoNotRemoveByVal()
        => VerifyAsync("""
            [|Module Program
                Sub Main(
                    ByVal _
                    args _
                    As String)
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main(
                    ByVal _
                    args _
                    As String)
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544561")]
    public Task NormalizeOperator_Text()
        => VerifyAsync("""
            [|Module Program
                Sub Main()
                    Dim z = 1
                    Dim y = 2
                    Dim x = z <   > y
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main()
                    Dim z = 1
                    Dim y = 2
                    Dim x = z <> y
                End Sub
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544557")]
    public Task NormalizeOperator_OperatorStatement()
        => VerifyAsync("""
            [|Class S
                Shared Operator >< (s1 As S, s2 As   S) As S
            End Class|]
            """, """
            Class S
                Shared Operator <>(s1 As S, s2 As S) As S
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544574")]
    public Task Reorder_OperatorTokenAndModifiers()
        => VerifyAsync("""
            [|Class S
                Shared Operator Widening CType(aa As S) As Byte
            End Class|]
            """, """
            Class S
                Shared Widening Operator CType(aa As S) As Byte
            End Class
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546521")]
    public Task SkippedTokenOperator()
        => VerifyAsync("""
            [|Module M
                Public Shared Narrowing Operator CTypeByVal s As Integer) As Test2
                    Return New Test2()
                End Operator
            End Module|]
            """, """
            Module M
                Public Shared Narrowing Operator CTypeByVal s As Integer) As Test2
                    Return New Test2()
                End Operator
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547255")]
    public Task ReorderAsyncModifier()
        => VerifyAsync("""
            [|Module M
                Public Async Function Goo() As Task(Of Integer)
                    Return 0
                End Function

                Async Public Function Goo2() As Task(Of Integer)
                    Return 0
                End Function

                Async Overridable Public Function Goo3() As Task(Of Integer)
                    Return 0
                End Function
            End Module|]
            """, """
            Module M
                Public Async Function Goo() As Task(Of Integer)
                    Return 0
                End Function

                Public Async Function Goo2() As Task(Of Integer)
                    Return 0
                End Function

                Public Overridable Async Function Goo3() As Task(Of Integer)
                    Return 0
                End Function
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547255")]
    public Task ReorderIteratorModifier()
        => VerifyAsync("""
            [|Module M
                Public Iterator Function Goo() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function

                Iterator Public Function Goo2() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function

                Iterator Overridable Public Function Goo3() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function
            End Module|]
            """, """
            Module M
                Public Iterator Function Goo() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function

                Public Iterator Function Goo2() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function

                Public Overridable Iterator Function Goo3() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611766")]
    public Task ReorderDuplicateModifiers()
        => VerifyAsync("""
            [|Module M
                Public Public Function Goo() As Integer
                    Return 0
                End Function

                Iterator Public Public Iterator Public Function Goo2() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function
            End Module|]
            """, """
            Module M
                Public Function Goo() As Integer
                    Return 0
                End Function

                Public Iterator Function Goo2() As IEnumerable(Of Integer)
                    Yield Return 0
                End Function
            End Module
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530058")]
    public Task TestBadOperatorToken()
        => VerifyAsync("""
            [|Module Test
            Class c1 
            Shared Operator ||(ByVal x As c1, ByVal y As c1) As Integer
            End Operator
            End Class
            End Module|]
            """, """
            Module Test
                Class c1
                    Shared Operator ||(ByVal x As c1, ByVal y As c1) As Integer
                    End Operator
                End Class
            End Module
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1534")]
    public Task TestColonEqualsToken()
        => VerifyAsync("""
            [|Module Program
                Sub Main(args As String())
                    Main(args   :     =    args)
                End Sub
            End Module|]
            """, """
            Module Program
                Sub Main(args As String())
                    Main(args:=args)
                End Sub
            End Module
            """);

    private static async Task VerifyAsync(string codeWithMarker, string expectedResult)
    {
        MarkupTestFile.GetSpans(codeWithMarker, out var codeWithoutMarker, out var textSpans);

        var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
        var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name is PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators or PredefinedCodeCleanupProviderNames.Format);

        var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], await document.GetCodeCleanupOptionsAsync(CancellationToken.None), codeCleanups);

        Assert.Equal(expectedResult, (await cleanDocument.GetSyntaxRootAsync()).ToFullString());
    }

    private static Document CreateDocument(string code, string language)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

        return project.AddDocument("Document", SourceText.From(code));
    }
}
