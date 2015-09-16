' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEvent
    Public Class GenerateEventTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateEventCodeFixProvider())
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestGenerateEventIntoInterface1()
            Test(
NewLines("Interface MyInterface \n End Interface \n Class C \n Implements MyInterface \n Event foo() Implements [|MyInterface.E|] \n End Class"),
NewLines("Interface MyInterface \n Event E() \n End Interface \n Class C \n Implements MyInterface \n Event foo() Implements MyInterface.E \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestNotIfIdentifierMissing()
            TestMissing(
NewLines("Interface MyInterface \n End Interface \n Class C \n Implements MyInterface \n Event foo() Implements [|MyInterface.|] \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestNotIfAlreadyPresent()
            TestMissing(
NewLines("Interface MyInterface \n Event E() \n End Interface \n Class C \n Implements MyInterface \n Event foo() Implements [|MyInterface.E|] \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestGenerateEventWithParameter()
            Test(
NewLines("Interface MyInterface \n End Interface \n Class C \n Implements MyInterface \n Event foo(x As Integer) Implements [|MyInterface.E|] \n End Class"),
NewLines("Interface MyInterface \n Event E(x As Integer) \n End Interface \n Class C \n Implements MyInterface \n Event foo(x As Integer) Implements MyInterface.E \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestHandlesClause()
            Test(
NewLines("Class D \n End Class \n Class C \n WithEvents a As D \n Sub bar(x As Integer, e As Object) Handles [|a.E|] \n End Sub \n End Class"),
NewLines("Class D \n Public Event E(x As Integer, e As Object) \n End Class \n Class C \n WithEvents a As D \n Sub bar(x As Integer, e As Object) Handles a.E \n End Sub \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestHandlesClauseWithExistingEvent()
            TestMissing(
NewLines("Class D \n Public Event E(x As Integer, e As Object) \n End Class \n Class C \n WithEvents a As D \n Sub bar(x As Integer, e As Object) Handles [|a.E|] \n End Sub \n End Class"))
        End Sub

        <WorkItem(531210)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestMyBase()
            Test(
NewLines("Public Class BaseClass \n ' Place methods and properties here. \n End Class \n  \n Public Class DerivedClass \n Inherits BaseClass \n Sub EventHandler(ByVal x As Integer) Handles [|MyBase.BaseEvent|] \n ' Place code to handle events from BaseClass here. \n End Sub \n End Class"),
NewLines("Public Class BaseClass \n Public Event BaseEvent(x As Integer) \n ' Place methods and properties here. \n End Class \n  \n Public Class DerivedClass \n Inherits BaseClass \n Sub EventHandler(ByVal x As Integer) Handles MyBase.BaseEvent \n ' Place code to handle events from BaseClass here. \n End Sub \n End Class"))
        End Sub

        <WorkItem(531210)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestMe()
            Test(
NewLines("Public Class C \n Sub EventHandler(ByVal x As Integer) Handles [|Me.MyEvent|] \n ' Place code to handle events from BaseClass here. \n End Sub \n End Class"),
NewLines("Public Class C \n Public Event MyEvent(x As Integer) \n Sub EventHandler(ByVal x As Integer) Handles Me.MyEvent \n ' Place code to handle events from BaseClass here. \n End Sub \n End Class"))
        End Sub

        <WorkItem(531210)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub TestMyClass()
            Test(
NewLines("Public Class C \n Sub EventHandler(ByVal x As Integer) Handles [|MyClass.MyEvent|] \n ' Place code to handle events from BaseClass here. \n End Sub \n End Class"),
NewLines("Public Class C \n Public Event MyEvent(x As Integer) \n Sub EventHandler(ByVal x As Integer) Handles MyClass.MyEvent \n ' Place code to handle events from BaseClass here. \n End Sub \n End Class"))
        End Sub

        <WorkItem(531251)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub NotIfEventMemberMissing()
            TestMissing(
NewLines("Public Class A \n End Class \n Public Class C \n Dim WithEvents x As A \n Sub Hello(i As Integer) Handles [|x.|]'mark \n End Sub \n End Class"))
        End Sub

        <WorkItem(531267)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub MakeParamsNotOptional()
            Test(
NewLines("Public Class B \n Dim WithEvents x As B \n Private Sub Test(Optional x As String = Nothing) Handles [|x.E1|] 'mark 1 \n End Sub \n Private Sub Test2(ParamArray x As String()) Handles x.E2 'mark 2 \n End Sub \n End Class"),
NewLines("Public Class B \n Dim WithEvents x As B \n Public Event E1(x As String) \n Private Sub Test(Optional x As String = Nothing) Handles x.E1 'mark 1 \n End Sub \n Private Sub Test2(ParamArray x As String()) Handles x.E2 'mark 2 \n End Sub \n End Class"))
        End Sub

        <WorkItem(531267)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub MakeParamsNotParamArray()
            Test(
NewLines("Public Class B \n Dim WithEvents x As B \n Private Sub Test(Optional x As String = Nothing) Handles x.E1 'mark 1 \n End Sub \n Private Sub Test2(ParamArray x As String()) Handles [|x.E2|] 'mark 2 \n End Sub \n End Class"),
NewLines("Public Class B \n Dim WithEvents x As B \n Public Event E2(x() As String) \n Private Sub Test(Optional x As String = Nothing) Handles x.E1 'mark 1 \n End Sub \n Private Sub Test2(ParamArray x As String()) Handles x.E2 'mark 2 \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventStaticClass()
            Test(
NewLines("Class EventClass \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler [|EventClass.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Class EventClass \n Public Event XEvent() \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler EventClass.XEvent, AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventStaticClass()
            Test(
NewLines("Class EventClass \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler [|EventClass.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Class EventClass \n Public Event XEvent() \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler EventClass.XEvent, AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventVariable()
            Test(
NewLines("Class EventClass \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler [|EClass.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Class EventClass \n Public Event XEvent() \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler EClass.XEvent, AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventVariable()
            Test(
NewLines("Class EventClass \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler [|EClass.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Class EventClass \n Public Event XEvent() \n Public Event ZEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler EClass.XEvent, AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEvent()
            Test(
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler [|XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler XEvent, AddressOf EClass_EventHandler \n End Sub \n Public Event XEvent() \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEvent()
            Test(
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler [|XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler XEvent, AddressOf EClass_EventHandler \n End Sub \n Public Event XEvent() \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMe()
            Test(
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler [|Me.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler Me.XEvent, AddressOf EClass_EventHandler \n End Sub \n Public Event XEvent() \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMe()
            Test(
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler [|Me.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler Me.XEvent, AddressOf EClass_EventHandler \n End Sub \n Public Event XEvent() \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMyClass()
            Test(
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler [|MyClass.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler MyClass.XEvent, AddressOf EClass_EventHandler \n End Sub \n Public Event XEvent() \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMyClass()
            Test(
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler [|MyClass.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler MyClass.XEvent, AddressOf EClass_EventHandler \n End Sub \n Public Event XEvent() \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMyBase()
            Test(
NewLines("Public Class EventClass \n End Class \n Public Class Test \n Inherits EventClass \n Public Sub New() \n AddHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class EventClass \n Public Event XEvent() \n End Class \n Public Class Test \n Inherits EventClass \n Public Sub New() \n AddHandler MyBase.XEvent, AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMyBase()
            Test(
NewLines("Public Class EventClass \n End Class \n Public Class Test \n Inherits EventClass \n Public Sub New() \n RemoveHandler [|MyBase.XEvent|], AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"),
NewLines("Public Class EventClass \n Public Event XEvent() \n End Class \n Public Class Test \n Inherits EventClass \n Public Sub New() \n RemoveHandler MyBase.XEvent, AddressOf EClass_EventHandler \n End Sub \n Sub EClass_EventHandler() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventDelegate()
            Test(
NewLines("Imports System \n Public Class EventClass \n End Class \n Public Class Test\n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler [|EClass.XEvent|], EClass_EventHandler \n End Sub \n Dim EClass_EventHandler As Action = Sub() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class EventClass \n Public Event XEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n AddHandler EClass.XEvent, EClass_EventHandler \n End Sub \n Dim EClass_EventHandler As Action = Sub() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventDelegate()
            Test(
NewLines("Imports System \n Public Class EventClass \n End Class \n Public Class Test\n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler [|EClass.XEvent|], EClass_EventHandler \n End Sub \n Dim EClass_EventHandler As Action = Sub() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class EventClass \n Public Event XEvent() \n End Class \n Public Class Test \n WithEvents EClass As New EventClass \n Public Sub New() \n RemoveHandler EClass.XEvent, EClass_EventHandler \n End Sub \n Dim EClass_EventHandler As Action = Sub() \n End Sub \n End Class"))
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMyBaseIntoCSharp()
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
            Test(initialMarkup, expected, compareTokens:=False)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMyBaseIntoCSharp()
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
            Test(initialMarkup, expected, compareTokens:=False)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMyBaseIntoCSharpGeneric()
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
            Test(initialMarkup, expected, compareTokens:=False)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMyBaseIntoCSharpGeneric()
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
            Test(initialMarkup, expected, compareTokens:=False)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMultiLineLambdaIntoCSharp()
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
            Test(initialMarkup, expected, compareTokens:=False)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMultiLineLambdaIntoCSharp()
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
            Test(initialMarkup, expected, compareTokens:=False)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForAddEventMyBaseIntoCSharpGenericExistingDelegate()
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
using System;

public class EventClass
{
    public event XEventHandler ZEvent;
}

public delegate void XEventHandler(object sender, EventArgs e);
</Document>
                    </Project>
                </Workspace>
            TestMissingWithWorkspaceXml(initialMarkup)
        End Sub

        <WorkItem(774321)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEvent)>
        Public Sub GenerateEventForRemoveEventMyBaseIntoCSharpGenericExistingDelegate()
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
using System;

public class EventClass
{
    public event XEventHandler ZEvent;
}

public delegate void XEventHandler(object sender, EventArgs e);
</Document>
                    </Project>
                </Workspace>

            TestMissingWithWorkspaceXml(initialMarkup)
        End Sub
    End Class
End Namespace
