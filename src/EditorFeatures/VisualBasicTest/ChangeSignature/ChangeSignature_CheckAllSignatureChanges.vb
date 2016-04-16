' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAllSignatureChanges_1This_3Regular_2Default() As Task
            Dim markup = <Text><![CDATA[
Option Strict On

Module Program
    ''' <summary>
    ''' See <see cref="M(String, Integer, String, Boolean, Integer, String)"/>
    ''' </summary>
    ''' <param name="o">o!</param>
    ''' <param name="a">a!</param>
    ''' <param name="b">b!</param>
    ''' <param name="c">c!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    <System.Runtime.CompilerServices.Extension>
    Sub $$M(ByVal o As String, a As Integer, b As String, c As Boolean, Optional x As Integer = 0, Optional y As String = "Zero")
        Dim t = "Test"

        M(t, 1, "Two", True, 3, "Four")
        t.M(1, "Two", True, 3, "Four")

        M(t, 1, "Two", True, 3)
        M(t, 1, "Two", True)

        M(t, 1, "Two", True, 3, y:="Four")
        M(t, 1, "Two", c:=True)

        M(t, 1, "Two", True, y:="Four")
        M(t, 1, "Two", True, x:=3)

        M(t, 1, "Two", True, y:="Four", x:=3)
        M(t, 1, y:="Four", x:=3, b:="Two", c:=True)
        M(t, y:="Four", x:=3, c:=True, b:="Two", a:=1)
        M(y:="Four", x:=3, c:=True, b:="Two", a:=1, o:=t)
    End Sub
End Module
]]></Text>.NormalizedValue()
            Dim signaturePartCounts = {1, 3, 2, 0}
            Await TestAllSignatureChangesAsync(LanguageNames.VisualBasic, markup, signaturePartCounts)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAllSignatureChanges_1This_3Regular_1ParamArray() As Task
            Dim markup = <Text><![CDATA[
Option Strict On

Module Program
    <System.Runtime.CompilerServices.Extension>
    Sub $$M(ByVal o As String, a As Integer, b As Boolean, c As Integer, ParamArray p As Integer())
        Dim t = "Test"

        M(t, 1, True, 3, {4, 5})
        t.M(1, True, 3, {4, 5})

        M(t, 1, True, 3, 4, 5)
        t.M(1, True, 3, 4, 5)

        M(t, 1, True, 3)
        t.M(1, True, 3)
    End Sub
End Module
]]></Text>.NormalizedValue()
            Dim signaturePartCounts = {1, 3, 0, 1}
            Await TestAllSignatureChangesAsync(LanguageNames.VisualBasic, markup, signaturePartCounts)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAllSignatureChanges_Delegate_3() As Task
            Dim markup = <Text><![CDATA[
Option Strict On

Class C
    ''' <summary>
    ''' <see cref="MyDelegate.Invoke(Integer, String, Boolean)"/>
    ''' <see cref="MyDelegate.BeginInvoke(Integer, String, Boolean, AsyncCallback, Object)"/>
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="y"></param>
    ''' <param name="z"></param>
    Delegate Sub $$MyDelegate(x As Integer, y As String, z As Boolean)

    Event MyEvent As MyDelegate
    Custom Event MyEvent2 As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(x As Integer, y As String, z As Boolean)
        End RaiseEvent
    End Event

    Sub M()
        RaiseEvent MyEvent(1, "Two", True)
        RaiseEvent MyEvent2(1, "Two", True)
        AddHandler MyEvent, AddressOf MyEventHandler
        AddHandler MyEvent2, AddressOf MyEventHandler2

        Dim x As MyDelegate = Nothing
        x(1, "Two", True)
        x.Invoke(1, "Two", True)
        x.BeginInvoke(1, "Two", True, Nothing, New Object())
    End Sub

    ''' <param name="a"></param>
    ''' <param name="b"></param>
    ''' <param name="c"></param>
    Sub MyEventHandler(a As Integer, b As String, c As Boolean)
    End Sub

    Sub MyEventHandlerEmpty()
    End Sub

    Sub MyEventHandler2(a As Integer, b As String, c As Boolean)
    End Sub

    Sub MyEventHandler2Empty()
    End Sub

    Sub MyOtherEventHandler(a As Integer, b As String, c As Boolean) Handles Me.MyEvent
    End Sub

    Sub MyOtherEventHandler2(a As Integer, b As String, c As Boolean) Handles Me.MyEvent2
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim signaturePartCounts = {0, 3, 0, 0}
            Await TestAllSignatureChangesAsync(LanguageNames.VisualBasic, markup, signaturePartCounts)
        End Function
    End Class
End Namespace
