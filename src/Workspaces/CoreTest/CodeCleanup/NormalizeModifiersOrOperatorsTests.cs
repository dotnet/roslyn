// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    [Trait(Traits.Feature, Traits.Features.NormalizeModifiersOrOperators)]
    public class NormalizeModifiersOrOperatorsTests
    {
        [Fact]
        public async Task PartialMethod()
        {
            var code = @"[|Class A
    Private Partial Sub()
    End Sub
End Class|]";

            var expected = @"Class A
    Partial Private Sub()
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task PartialClass()
        {
            var code = @"[|Public Partial Class A
End Class|]";

            var expected = @"Partial Public Class A
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task DefaultProperty()
        {
            var code = @"[|Class Class1
    Public Default Property prop1(i As Integer) As Integer
        Get
            Return i
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class|]";

            var expected = @"Class Class1
    Default Public Property prop1(i As Integer) As Integer
        Get
            Return i
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Accessors()
        {
            var code = @"[|Public Module M
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
End Class|]";

            var expected = @"Public Module M
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
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Structure()
        {
            var code = @"[|Public Partial Structure S
End Structure|]";

            var expected = @"Partial Public Structure S
End Structure";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Interface()
        {
            var code = @"[|Public Interface O
    Public Interface S
    End Interface
End Interface

Public Interface O2
    Inherits O

    Shadows Public Interface S
    End Interface
End Interface|]";

            var expected = @"Public Interface O
    Public Interface S
    End Interface
End Interface

Public Interface O2
    Inherits O

    Public Shadows Interface S
    End Interface
End Interface";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Class()
        {
            var code = @"[|MustInherit Public  Class C
End Class|]";

            var expected = @"Public MustInherit Class C
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Enum()
        {
            var code = @"[|Public Class O
    Public Enum S
        None
    End Enum
End Class

Public Class O2
    Inherits O

    Shadows Public  Enum S
        None
    End Enum
End Class|]";

            var expected = @"Public Class O
    Public Enum S
        None
    End Enum
End Class

Public Class O2
    Inherits O

    Public Shadows Enum S
        None
    End Enum
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Method()
        {
            var code = @"[|Public Class O
    Overridable Protected Function Test() As Integer
        Return 0
    End Function
End Class|]";

            var expected = @"Public Class O
    Protected Overridable Function Test() As Integer
        Return 0
    End Function
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Declare()
        {
            var code = @"[|Class C
    Overloads Public  Declare Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA"" (ByVal lpBuffer As String, ByRef nSize As Integer) As Integer
End Class|]";

            var expected = @"Class C
    Public Overloads Declare Function getUserName Lib ""advapi32.dll"" Alias ""GetUserNameA"" (ByVal lpBuffer As String, ByRef nSize As Integer) As Integer
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Delegate()
        {
            var code = @"[|Public Class O
    Public Delegate Function S() As Integer
End Class

Public Class O2
    Inherits O

    Shadows Public  Delegate Function S() As Integer
End Class|]";

            var expected = @"Public Class O
    Public Delegate Function S() As Integer
End Class

Public Class O2
    Inherits O

    Public Shadows Delegate Function S() As Integer
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Event()
        {
            var code = @"[|Public Class O
    Shared Public  Event Test As System.EventHandler
End Class|]";

            var expected = @"Public Class O
    Public Shared Event Test As System.EventHandler
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Operator()
        {
            var code = @"[|Public Structure abc
    Shared Overloads Public  Operator And(ByVal x As abc, ByVal y As abc) As abc
    End Operator
End Structure|]";

            var expected = @"Public Structure abc
    Public Overloads Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
    End Operator
End Structure";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Property()
        {
            var code = @"[|Class Class1
   Overridable  Public  Property prop1 As Integer
End Class|]";

            var expected = @"Class Class1
    Public Overridable Property prop1 As Integer
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Accessor()
        {
            var code = @"[|Class Class1
    Public Property prop1 As Integer
        Private Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class|]";

            var expected = @"Class Class1
    Public Property prop1 As Integer
        Private Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task IncompleteMember()
        {
            var code = @"[|Class Program
    Shared Private Dim
End Class|]";

            var expected = @"Class Program
    Shared Private Dim
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Field()
        {
            var code = @"[|Class Program
    Shared ReadOnly Private Dim f = 1
End Class|]";

            var expected = @"Class Program
    Private Shared ReadOnly f = 1
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task NotOverridable_Overridable_Overrides()
        {
            var code = @"[|Public Class Program
    Class N
        Inherits Program

        Overrides Public   NotOverridable Sub test()
            MyBase.test()
        End Sub
    End Class

    Overridable Public  Sub test()
    End Sub
End Class|]";

            var expected = @"Public Class Program
    Class N
        Inherits Program

        Public NotOverridable Overrides Sub test()
            MyBase.test()
        End Sub
    End Class

    Public Overridable Sub test()
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task MustOverride_MustInherit()
        {
            var code = @"[|MustInherit Public Class Program
    MustOverride Public Sub test()
End Class|]";

            var expected = @"Public MustInherit Class Program
    Public MustOverride Sub test()
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Overloads()
        {
            var code = @"[|Public MustInherit Class Program
   Overloads Public  Sub test()
    End Sub

    Overloads Public  Sub test(i As Integer)
    End Sub
End Class|]";

            var expected = @"Public MustInherit Class Program
    Public Overloads Sub test()
    End Sub

    Public Overloads Sub test(i As Integer)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task NotInheritable()
        {
            var code = @"[|NotInheritable Public Class Program
End Class|]";

            var expected = @"Public NotInheritable Class Program
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Shared_Shadow_ReadOnly_Const()
        {
            var code = @"[|Class C
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
End Class|]";

            var expected = @"Class C
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
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task WriteOnly()
        {
            var code = @"[|Class C
    WriteOnly Public  Property Test
        Set(value)
        End Set
    End Property
End Class|]";

            var expected = @"Class C
    Public WriteOnly Property Test
        Set(value)
        End Set
    End Property
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task WithEvent_Custom_Dim()
        {
            var code = @"[|Imports System

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
End Class|]";

            var expected = @"Imports System

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
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Widening_Narrowing()
        {
            var code = @"[|Public Structure digit
Widening  Shared  Public Operator CType(ByVal d As digit) As Byte
        Return 0
    End Operator
     Narrowing Public Shared  Operator CType(ByVal b As Byte) As digit
        Return Nothing
    End Operator
End Structure|]";

            var expected = @"Public Structure digit
    Public Shared Widening Operator CType(ByVal d As digit) As Byte
        Return 0
    End Operator
    Public Shared Narrowing Operator CType(ByVal b As Byte) As digit
        Return Nothing
    End Operator
End Structure";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task Static_Const_Dim()
        {
            var code = @"[|Class A
    Sub Method()
        Dim Static a As Integer = 1
        Const a2 As Integer = 2
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Method()
        Static Dim a As Integer = 1
        Const a2 As Integer = 2
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
        public async Task RemoveByVal1()
        {
            var code = @"[|Class A
    Sub Method(ByVal t As String)
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Method(ByVal t As String)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
        public async Task RemoveByVal2()
        {
            var code = @"[|Class A
    Sub Method(ByVal t As String, ByRef t1 As String)
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Method(ByVal t As String, ByRef t1 As String)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
        public async Task RemoveByVal_LineContinuation()
        {
            var code = @"[|Class A
    Sub Method( _
        ByVal _
              _
            t As String, ByRef t1 As String)
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Method( _
        ByVal _
              _
            t As String, ByRef t1 As String)
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task RemoveDim()
        {
            var code = @"[|Class A
    Dim  Shared Private a As Integer = 1
End Class|]";

            var expected = @"Class A
    Private Shared a As Integer = 1
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task RemoveDim_LineContinuation()
        {
            var code = @"[|Class A
    Dim _
        Shared _
        Private _
            a As Integer = 1
End Class|]";

            var expected = @"Class A
    Private _
        Shared _
               _
            a As Integer = 1
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task LessThanGreaterThan()
        {
            var code = @"[|Class A
    Sub Test()
        If 1 >< 2 Then
        End If
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Test()
        If 1 <> 2 Then
        End If
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task GreaterThanEquals()
        {
            var code = @"[|Class A
    Sub Test()
        If 1 => 2 Then
        End If
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Test()
        If 1 >= 2 Then
        End If
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task LessThanEquals()
        {
            var code = @"[|Class A
    Sub Test()
        If 1 =< 2 Then
        End If
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Test()
        If 1 <= 2 Then
        End If
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact]
        public async Task LessThanEquals_LineContinuation()
        {
            var code = @"[|Class A
    Sub Test()
        If 1 _ 
            = _ 
            < _
                2 Then
        End If
    End Sub
End Class|]";

            var expected = @"Class A
    Sub Test()
        If 1 _
            <= _
                2 Then
        End If
    End Sub
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544300")]
        public async Task NormalizedOperator_StructuredTrivia()
        {
            var code = @"[|#If VBC_VER => 9.0|]";

            var expected = @"#If VBC_VER >= 9.0";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544520")]
        public async Task DoNotRemoveByVal()
        {
            var code = @"[|Module Program
    Sub Main(
        ByVal _
        args _
        As String)
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main(
        ByVal _
        args _
        As String)
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544561")]
        public async Task NormalizeOperator_Text()
        {
            var code = @"[|Module Program
    Sub Main()
        Dim z = 1
        Dim y = 2
        Dim x = z <   > y
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main()
        Dim z = 1
        Dim y = 2
        Dim x = z <> y
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544557")]
        public async Task NormalizeOperator_OperatorStatement()
        {
            var code = @"[|Class S
    Shared Operator >< (s1 As S, s2 As   S) As S
End Class|]";

            var expected = @"Class S
    Shared Operator <>(s1 As S, s2 As S) As S
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544574")]
        public async Task Reorder_OperatorTokenAndModifiers()
        {
            var code = @"[|Class S
    Shared Operator Widening CType(aa As S) As Byte
End Class|]";

            var expected = @"Class S
    Shared Widening Operator CType(aa As S) As Byte
End Class";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546521")]
        public async Task SkippedTokenOperator()
        {
            var code = @"[|Module M
    Public Shared Narrowing Operator CTypeByVal s As Integer) As Test2
        Return New Test2()
    End Operator
End Module|]";

            var expected = @"Module M
    Public Shared Narrowing Operator CTypeByVal s As Integer) As Test2
        Return New Test2()
    End Operator
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547255")]
        public async Task ReorderAsyncModifier()
        {
            var code = @"[|Module M
    Public Async Function Goo() As Task(Of Integer)
        Return 0
    End Function

    Async Public Function Goo2() As Task(Of Integer)
        Return 0
    End Function

    Async Overridable Public Function Goo3() As Task(Of Integer)
        Return 0
    End Function
End Module|]";

            var expected = @"Module M
    Public Async Function Goo() As Task(Of Integer)
        Return 0
    End Function

    Public Async Function Goo2() As Task(Of Integer)
        Return 0
    End Function

    Public Overridable Async Function Goo3() As Task(Of Integer)
        Return 0
    End Function
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547255")]
        public async Task ReorderIteratorModifier()
        {
            var code = @"[|Module M
    Public Iterator Function Goo() As IEnumerable(Of Integer)
        Yield Return 0
    End Function

    Iterator Public Function Goo2() As IEnumerable(Of Integer)
        Yield Return 0
    End Function

    Iterator Overridable Public Function Goo3() As IEnumerable(Of Integer)
        Yield Return 0
    End Function
End Module|]";

            var expected = @"Module M
    Public Iterator Function Goo() As IEnumerable(Of Integer)
        Yield Return 0
    End Function

    Public Iterator Function Goo2() As IEnumerable(Of Integer)
        Yield Return 0
    End Function

    Public Overridable Iterator Function Goo3() As IEnumerable(Of Integer)
        Yield Return 0
    End Function
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611766")]
        public async Task ReorderDuplicateModifiers()
        {
            var code = @"[|Module M
    Public Public Function Goo() As Integer
        Return 0
    End Function

    Iterator Public Public Iterator Public Function Goo2() As IEnumerable(Of Integer)
        Yield Return 0
    End Function
End Module|]";

            var expected = @"Module M
    Public Function Goo() As Integer
        Return 0
    End Function

    Public Iterator Function Goo2() As IEnumerable(Of Integer)
        Yield Return 0
    End Function
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530058")]
        public async Task TestBadOperatorToken()
        {
            var code = @"[|Module Test
Class c1 
Shared Operator ||(ByVal x As c1, ByVal y As c1) As Integer
End Operator
End Class
End Module|]";

            var expected = @"Module Test
    Class c1
        Shared Operator ||(ByVal x As c1, ByVal y As c1) As Integer
        End Operator
    End Class
End Module";

            await VerifyAsync(code, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1534")]
        public async Task TestColonEqualsToken()
        {
            var code = @"[|Module Program
    Sub Main(args As String())
        Main(args   :     =    args)
    End Sub
End Module|]";

            var expected = @"Module Program
    Sub Main(args As String())
        Main(args:=args)
    End Sub
End Module";

            await VerifyAsync(code, expected);
        }

        private static async Task VerifyAsync(string codeWithMarker, string expectedResult)
        {
            MarkupTestFile.GetSpans(codeWithMarker, out var codeWithoutMarker, out var textSpans);

            var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
            var codeCleanups = CodeCleaner.GetDefaultProviders(document).WhereAsArray(p => p.Name is PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators or PredefinedCodeCleanupProviderNames.Format);

            var cleanDocument = await CodeCleaner.CleanupAsync(document, textSpans[0], CodeCleanupOptions.GetDefault(document.Project.Services), codeCleanups);

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
}
