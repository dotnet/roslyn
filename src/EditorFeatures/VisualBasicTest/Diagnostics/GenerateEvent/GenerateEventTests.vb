' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEvent
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
    Public Class GenerateEventTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New GenerateEventCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestGenerateEventIntoInterface1() As Task
            Await TestInRegularAndScriptAsync(
"Interface MyInterface
End Interface
Class C
    Implements MyInterface
    Event goo() Implements [|MyInterface.E|]
End Class",
"Interface MyInterface
    Event E()
End Interface
Class C
    Implements MyInterface
    Event goo() Implements MyInterface.E
End Class")
        End Function

        <Fact>
        Public Async Function TestNotIfIdentifierMissing() As Task
            Await TestMissingInRegularAndScriptAsync(
"Interface MyInterface
End Interface
Class C
    Implements MyInterface
    Event goo() Implements [|MyInterface.|] 
 End Class")
        End Function

        <Fact>
        Public Async Function TestNotIfAlreadyPresent() As Task
            Await TestMissingInRegularAndScriptAsync(
"Interface MyInterface
    Event E()
End Interface
Class C
    Implements MyInterface
    Event goo() Implements [|MyInterface.E|]
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateEventWithParameter() As Task
            Await TestInRegularAndScriptAsync(
"Interface MyInterface
End Interface
Class C
    Implements MyInterface
    Event goo(x As Integer) Implements [|MyInterface.E|]
End Class",
"Interface MyInterface
    Event E(x As Integer)
End Interface
Class C
    Implements MyInterface
    Event goo(x As Integer) Implements MyInterface.E
End Class")
        End Function

        <Fact>
        Public Async Function TestHandlesClause() As Task
            Await TestInRegularAndScriptAsync(
"Class D
End Class
Class C
    WithEvents a As D
    Sub bar(x As Integer, e As Object) Handles [|a.E|]
    End Sub
End Class",
"Class D
    Public Event E(x As Integer, e As Object)
End Class
Class C
    WithEvents a As D
    Sub bar(x As Integer, e As Object) Handles a.E
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestHandlesClauseWithExistingEvent() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class D
    Public Event E(x As Integer, e As Object)
End Class
Class C
    WithEvents a As D
    Sub bar(x As Integer, e As Object) Handles [|a.E|]
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531210")>
        Public Async Function TestMyBase() As Task
            Await TestInRegularAndScriptAsync(
"Public Class BaseClass
    ' Place methods and properties here. 
End Class

Public Class DerivedClass
    Inherits BaseClass
    Sub EventHandler(ByVal x As Integer) Handles [|MyBase.BaseEvent|]
        ' Place code to handle events from BaseClass here. 
    End Sub
End Class",
"Public Class BaseClass
    Public Event BaseEvent(x As Integer)
    ' Place methods and properties here. 
End Class

Public Class DerivedClass
    Inherits BaseClass
    Sub EventHandler(ByVal x As Integer) Handles MyBase.BaseEvent
        ' Place code to handle events from BaseClass here. 
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531210")>
        Public Async Function TestMe() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub EventHandler(ByVal x As Integer) Handles [|Me.MyEvent|]
        ' Place code to handle events from BaseClass here. 
    End Sub
End Class",
"Public Class C
    Public Event MyEvent(x As Integer)

    Sub EventHandler(ByVal x As Integer) Handles Me.MyEvent
        ' Place code to handle events from BaseClass here. 
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531210")>
        Public Async Function TestMyClass() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub EventHandler(ByVal x As Integer) Handles [|MyClass.MyEvent|]
        ' Place code to handle events from BaseClass here. 
    End Sub
End Class",
"Public Class C
    Public Event MyEvent(x As Integer)

    Sub EventHandler(ByVal x As Integer) Handles MyClass.MyEvent
        ' Place code to handle events from BaseClass here. 
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531251")>
        Public Async Function TestNotIfEventMemberMissing() As Task
            Await TestMissingInRegularAndScriptAsync(
"Public Class A
End Class
Public Class C
    Dim WithEvents x As A
    Sub Hello(i As Integer) Handles [|x.|]'mark 
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531267")>
        Public Async Function TestMakeParamsNotOptional() As Task
            Await TestInRegularAndScriptAsync(
"Public Class B
    Dim WithEvents x As B
    Private Sub Test(Optional x As String = Nothing) Handles [|x.E1|] 'mark 1 
    End Sub
    Private Sub Test2(ParamArray x As String()) Handles x.E2 'mark 2 
    End Sub
End Class",
"Public Class B
    Dim WithEvents x As B
    Public Event E1(x As String)

    Private Sub Test(Optional x As String = Nothing) Handles x.E1 'mark 1 
    End Sub
    Private Sub Test2(ParamArray x As String()) Handles x.E2 'mark 2 
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531267")>
        Public Async Function TestMakeParamsNotParamArray() As Task
            Await TestInRegularAndScriptAsync(
"Public Class B
    Dim WithEvents x As B
    Private Sub Test(Optional x As String = Nothing) Handles x.E1 'mark 1 
    End Sub
    Private Sub Test2(ParamArray x As String()) Handles [|x.E2|] 'mark 2 
    End Sub
End Class",
"Public Class B
    Dim WithEvents x As B
    Public Event E2(x() As String)

    Private Sub Test(Optional x As String = Nothing) Handles x.E1 'mark 1 
    End Sub
    Private Sub Test2(ParamArray x As String()) Handles x.E2 'mark 2 
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventStaticClass() As Task
            Await TestInRegularAndScriptAsync(
"Class EventClass
    Public Event ZEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|EventClass.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Class EventClass
    Public Event ZEvent()
    Public Event XEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler EventClass.XEvent, AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventStaticClass() As Task
            Await TestInRegularAndScriptAsync(
"Class EventClass
    Public Event ZEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|EventClass.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Class EventClass
    Public Event ZEvent()
    Public Event XEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler EventClass.XEvent, AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventVariable() As Task
            Await TestInRegularAndScriptAsync(
"Class EventClass
    Public Event ZEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|EClass.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Class EventClass
    Public Event ZEvent()
    Public Event XEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler EClass.XEvent, AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventVariable() As Task
            Await TestInRegularAndScriptAsync(
"Class EventClass
    Public Event ZEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|EClass.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Class EventClass
    Public Event ZEvent()
    Public Event XEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler EClass.XEvent, AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEvent() As Task
            Await TestInRegularAndScriptAsync(
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler XEvent, AddressOf EClass_EventHandler
    End Sub

    Public Event XEvent()

    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEvent() As Task
            Await TestInRegularAndScriptAsync(
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler XEvent, AddressOf EClass_EventHandler
    End Sub

    Public Event XEvent()

    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMe() As Task
            Await TestInRegularAndScriptAsync(
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|Me.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler Me.XEvent, AddressOf EClass_EventHandler
    End Sub

    Public Event XEvent()

    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMe() As Task
            Await TestInRegularAndScriptAsync(
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|Me.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler Me.XEvent, AddressOf EClass_EventHandler
    End Sub

    Public Event XEvent()

    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMyClass() As Task
            Await TestInRegularAndScriptAsync(
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|MyClass.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler MyClass.XEvent, AddressOf EClass_EventHandler
    End Sub

    Public Event XEvent()

    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMyClass() As Task
            Await TestInRegularAndScriptAsync(
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|MyClass.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler MyClass.XEvent, AddressOf EClass_EventHandler
    End Sub

    Public Event XEvent()

    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMyBase() As Task
            Await TestInRegularAndScriptAsync(
"Public Class EventClass
End Class
Public Class Test
    Inherits EventClass
    Public Sub New()
        AddHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class EventClass
    Public Event XEvent()
End Class
Public Class Test
    Inherits EventClass
    Public Sub New()
        AddHandler MyBase.XEvent, AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMyBase() As Task
            Await TestInRegularAndScriptAsync(
"Public Class EventClass
End Class
Public Class Test
    Inherits EventClass
    Public Sub New()
        RemoveHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class",
"Public Class EventClass
    Public Event XEvent()
End Class
Public Class Test
    Inherits EventClass
    Public Sub New()
        RemoveHandler MyBase.XEvent, AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventDelegate() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class EventClass
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|EClass.XEvent|], EClass_EventHandler
    End Sub
    Dim EClass_EventHandler As Action = Sub()
                                        End Sub
End Class",
"Imports System
Public Class EventClass
    Public Event XEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler EClass.XEvent, EClass_EventHandler
    End Sub
    Dim EClass_EventHandler As Action = Sub()
                                        End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventDelegate() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class EventClass
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|EClass.XEvent|], EClass_EventHandler
    End Sub
    Dim EClass_EventHandler As Action = Sub()
                                        End Sub
End Class",
"Imports System
Public Class EventClass
    Public Event XEvent()
End Class
Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler EClass.XEvent, EClass_EventHandler
    End Sub
    Dim EClass_EventHandler As Action = Sub()
                                        End Sub
End Class")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMyBaseIntoCSharp() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Public Class Test
    Inherits EventClass
    Public Sub New()
        AddHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler(argument As String)
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
public class EventClass
{
}
                        </Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Text>
public class EventClass
{
    public event XEventHandler XEvent;
}

public delegate void XEventHandler(string argument);
</Text>.NormalizedValue
            Await TestInRegularAndScriptAsync(initialMarkup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMyBaseIntoCSharp() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Public Class Test
    Inherits EventClass
    Public Sub New()
        RemoveHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler
    End Sub
    Sub EClass_EventHandler(argument As String)
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
public class EventClass
{
}
                        </Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Text>
public class EventClass
{
    public event XEventHandler XEvent;
}

public delegate void XEventHandler(string argument);
</Text>.NormalizedValue
            Await TestInRegularAndScriptAsync(initialMarkup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMyBaseIntoCSharpGeneric() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Imports System

Public Class Test
    Inherits EventClass
    Public Sub New()
        AddHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler(Of EventArgs)
    End Sub
    Sub EClass_EventHandler(Of T)(sender As Object, e As T)
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
public class EventClass
{
}
</Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Text>
using System;

public class EventClass
{
    public event XEventHandler XEvent;
}

public delegate void XEventHandler(object sender, EventArgs e);
</Text>.NormalizedValue
            Await TestInRegularAndScriptAsync(initialMarkup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMyBaseIntoCSharpGeneric() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Imports System

Public Class Test
    Inherits EventClass
    Public Sub New()
        RemoveHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler(Of EventArgs)
    End Sub
    Sub EClass_EventHandler(Of T)(sender As Object, e As T)
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
public class EventClass
{
}
</Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Text>
using System;

public class EventClass
{
    public event XEventHandler XEvent;
}

public delegate void XEventHandler(object sender, EventArgs e);
</Text>.NormalizedValue
            Await TestInRegularAndScriptAsync(initialMarkup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMultiLineLambdaIntoCSharp() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Imports System

Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        AddHandler [|EClass.XEvent|], Sub(a As Object, b As EventArgs)
                                  End Sub
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
public class EventClass
{
}
</Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Text>
using System;

public class EventClass
{
    public event XEventHandler XEvent;
}

public delegate void XEventHandler(object a, EventArgs b);
</Text>.NormalizedValue
            Await TestInRegularAndScriptAsync(initialMarkup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMultiLineLambdaIntoCSharp() As Task
            Dim initialMarkup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Imports System

Public Class Test
    WithEvents EClass As New EventClass
    Public Sub New()
        RemoveHandler [|EClass.XEvent|], Sub(a As Object, b As EventArgs)
                                  End Sub
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
public class EventClass
{
}
</Document>
                    </Project>
                </Workspace>.ToString()
            Dim expected =
                <Text>
using System;

public class EventClass
{
    public event XEventHandler XEvent;
}

public delegate void XEventHandler(object a, EventArgs b);
</Text>.NormalizedValue
            Await TestInRegularAndScriptAsync(initialMarkup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForAddEventMyBaseIntoCSharpGenericExistingDelegate() As Task
            Dim initialMarkup =
                "<Workspace>
                    <Project Language=""Visual Basic"" CommonReferences=""true"">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Imports System

Public Class Test
    Inherits EventClass
    Public Sub New()
        AddHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler(Of EventArgs)
    End Sub
    Sub EClass_EventHandler(Of T)(sender As Object, e As T)
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
                        <Document>
using System;

public class EventClass
{
    public event XEventHandler ZEvent;
}

public delegate void XEventHandler(object sender, EventArgs e);
</Document>
                    </Project>
                </Workspace>"
            Await TestMissingInRegularAndScriptAsync(initialMarkup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
        Public Async Function TestGenerateEventForRemoveEventMyBaseIntoCSharpGenericExistingDelegate() As Task
            Dim initialMarkup =
                "<Workspace>
                    <Project Language=""Visual Basic"" CommonReferences=""True"">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document>
Imports System

Public Class Test
    Inherits EventClass
    Public Sub New()
        RemoveHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler(Of EventArgs)
    End Sub
    Sub EClass_EventHandler(Of T)(sender As Object, e As T)
    End Sub
End Class
</Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""True"">
                        <Document>
using System;

public class EventClass
{
    public event XEventHandler ZEvent;
}

public delegate void XEventHandler(object sender, EventArgs e);
</Document>
                    </Project>
                </Workspace>"

            Await TestMissingInRegularAndScriptAsync(initialMarkup)
        End Function
    End Class
End Namespace
