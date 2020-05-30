﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_ImplicitInvokeCalls() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s(True, 12345, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature,
                                                     expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode, expectedSelectedIndex:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_ExplicitInvokeCalls() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub MySub($$x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.Invoke(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.Invoke(True, 12345, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature,
                                                     expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode, expectedSelectedIndex:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_BeginInvokeCalls() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub MySub(x As Integer$$, y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.BeginInvoke(1, "Two", True, Nothing, Nothing)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.BeginInvoke(True, 12345, "Two", Nothing, Nothing)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature,
                                                     expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode, expectedSelectedIndex:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_SubLambdas() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub MySub(x As Integer, $$y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing

        s = Sub()
            End Sub

        s = Sub(a As Integer, b As String, c As Boolean)
            End Sub

        s = Sub(a As Integer, b As String, c As Boolean) System.Console.WriteLine("Test")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing

        s = Sub()
            End Sub

        s = Sub(c As Boolean, newIntegerParameter As Integer, b As String)
            End Sub

        s = Sub(c As Boolean, newIntegerParameter As Integer, b As String) System.Console.WriteLine("Test")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature,
                                                     expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode, expectedSelectedIndex:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_FunctionLambdas() As Task
            Dim markup = <Text><![CDATA[
Delegate Function MyFunc(x As Integer, y As String, $$z As Boolean) As Integer

Class C
    Sub M()
        Dim f As MyFunc = Nothing

        f = Function()
                Return 1
            End Function

        f = Function(a As Integer, b As String, c As Boolean)
                Return 1
            End Function

        f = Function(a As Integer, b As String, c As Boolean) 1
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, newIntegerParameter As Integer, y As String) As Integer

Class C
    Sub M()
        Dim f As MyFunc = Nothing

        f = Function()
                Return 1
            End Function

        f = Function(c As Boolean, newIntegerParameter As Integer, b As String)
                Return 1
            End Function

        f = Function(c As Boolean, newIntegerParameter As Integer, b As String) 1
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature,
                                                     expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode, expectedSelectedIndex:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_ReferencingLambdas_MethodArgument() As Task
            Dim markup = <Text><![CDATA[
Delegate Function $$MyFunc(x As Integer, y As String, z As Boolean) As Integer

Class C
    Sub M(f As MyFunc)
        M(Function(a As Integer, b As String, c As Boolean) 1)

        M(Function(a As Integer, b As String, c As Boolean)
              Return 1
          End Function)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, newIntegerParameter As Integer, y As String) As Integer

Class C
    Sub M(f As MyFunc)
        M(Function(c As Boolean, newIntegerParameter As Integer, b As String) 1)

        M(Function(c As Boolean, newIntegerParameter As Integer, b As String)
              Return 1
          End Function)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_ReferencingLambdas_ReturnValue() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

Class C
    Function M1() As MySub
        Return Sub(a As Integer, b As String, c As Boolean)
               End Sub
    End Function

    Function M2() As MySub
        Return Sub(a As Integer, b As String, c As Boolean) System.Console.WriteLine("Test")
    End Function
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Function M1() As MySub
        Return Sub(c As Boolean, newIntegerParameter As Integer, b As String)
               End Sub
    End Function

    Function M2() As MySub
        Return Sub(c As Boolean, newIntegerParameter As Integer, b As String) System.Console.WriteLine("Test")
    End Function
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_Recursive() As Task
            Dim markup = <Text><![CDATA[
Delegate Function $$MyFunc(x As Integer, y As String, z As Boolean) As MyFunc

Class C
    Sub M()
        Dim f As MyFunc = Nothing
        f(1, "Two", True)(1, "Two", True)(1, "Two", True)(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, newIntegerParameter As Integer, y As String) As MyFunc

Class C
    Sub M()
        Dim f As MyFunc = Nothing
        f(True, 12345, "Two")(True, 12345, "Two")(True, 12345, "Two")(True, 12345, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_DocComments() As Task
            Dim markup = <Text><![CDATA[
''' <summary>
'''   This is <see cref="MyFunc"/>, which has these methods:
'''     <see cref="MyFunc.New(Object, System.IntPtr)"/>
'''     <see cref="MyFunc.Invoke(Integer, String, Boolean)"/>
'''     <see cref="MyFunc.EndInvoke(System.IAsyncResult)"/>
'''     <see cref="MyFunc.BeginInvoke(Integer, String, Boolean, System.AsyncCallback, Object)"/>
''' </summary>
''' <param name="x">x!</param>
''' <param name="y">y!</param>
''' <param name="z">z!</param>
Delegate Sub $$MyFunc(x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim f As MyFunc = AddressOf Test
    End Sub

    ''' <param name="a"></param>
    ''' <param name="b"></param>
    ''' <param name="c"></param>
    Private Sub Test(a As Integer, b As String, c As Boolean)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
''' <summary>
'''   This is <see cref="MyFunc"/>, which has these methods:
'''     <see cref="MyFunc.New(Object, System.IntPtr)"/>
'''     <see cref="MyFunc.Invoke(Boolean, Integer, String)"/>
'''     <see cref="MyFunc.EndInvoke(System.IAsyncResult)"/>
'''     <see cref="MyFunc.BeginInvoke(Boolean, Integer, String, System.AsyncCallback, Object)"/>
''' </summary>
''' <param name="z">z!</param>
''' <param name="newIntegerParameter"></param>
''' <param name="y">y!</param>
Delegate Sub MyFunc(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Sub M()
        Dim f As MyFunc = AddressOf Test
    End Sub

    ''' <param name="c"></param>
    ''' <param name="newIntegerParameter"></param>
    ''' <param name="b"></param>
    Private Sub Test(c As Boolean, newIntegerParameter As Integer, b As String)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_Relaxation_FunctionToSub() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim f As MySub = AddressOf Test
    End Sub

    Private Function Test(a As Integer, b As String, c As Boolean) As Integer
        Return 1
    End Function
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, newIntegerParameter As Integer, y As String)

Class C
    Sub M()
        Dim f As MySub = AddressOf Test
    End Sub

    Private Function Test(c As Boolean, newIntegerParameter As Integer, b As String) As Integer
        Return 1
    End Function
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_Relaxation_ParameterlessFunctionToFunction() As Task
            Dim markup = <Text><![CDATA[
Delegate Function $$MyFunc(x As Integer, y As String, z As Boolean) As Integer

Class C
    Sub M()
        Dim f As MyFunc = AddressOf Test
    End Sub

    Private Function Test()
        Return 1
    End Function
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, newIntegerParameter As Integer, y As String) As Integer

Class C
    Sub M()
        Dim f As MyFunc = AddressOf Test
    End Sub

    Private Function Test()
        Return 1
    End Function
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_CascadeToEvents() As Task
            Dim markup = <Text><![CDATA[
Class C
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
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, newIntegerParameter As Integer, y As String)

    Event MyEvent As MyDelegate
    Custom Event MyEvent2 As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, newIntegerParameter As Integer, y As String)
        End RaiseEvent
    End Event

    Sub M()
        RaiseEvent MyEvent(True, 12345, "Two")
        RaiseEvent MyEvent2(True, 12345, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Events_ReferencedBy_RaiseEvent() As Task
            Dim markup = <Text><![CDATA[
Class C
    Event $$MyEvent(x As Integer, y As String, z As Boolean)

    Sub M()
        RaiseEvent MyEvent(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, newIntegerParameter As Integer, y As String)

    Sub M()
        RaiseEvent MyEvent(True, 12345, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Events_ReferencedBy_AddHandler() As Task
            Dim markup = <Text><![CDATA[
Class C
    Event $$MyEvent(x As Integer, y As String, z As Boolean)

    Sub M()
        AddHandler MyEvent, AddressOf MyEventHandler
    End Sub

    Sub MyEventHandler(a As Integer, b As String, c As Boolean)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, newIntegerParameter As Integer, y As String)

    Sub M()
        AddHandler MyEvent, AddressOf MyEventHandler
    End Sub

    Sub MyEventHandler(c As Boolean, newIntegerParameter As Integer, b As String)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Events_ReferencedBy_GeneratedDelegateTypeInvocations() As Task
            Dim markup = <Text><![CDATA[
Class C
    Event $$MyEvent(x As Integer, y As String, z As Boolean)

    Sub M()
        Dim e As MyEventEventHandler = Nothing
        e(1, "Two", True)
        e.Invoke(1, "Two", True)
        e.BeginInvoke(1, "Two", True, Nothing, New Object())
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, newIntegerParameter As Integer, y As String)

    Sub M()
        Dim e As MyEventEventHandler = Nothing
        e(True, 12345, "Two")
        e.Invoke(True, 12345, "Two")
        e.BeginInvoke(True, 12345, "Two", Nothing, New Object())
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Events_ReferencedBy_HandlesClause() As Task
            Dim markup = <Text><![CDATA[
Class C
    Event $$MyEvent(x As Integer, y As String, z As Boolean)

    Sub MyOtherEventHandler(a As Integer, b As String, c As Boolean) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, newIntegerParameter As Integer, y As String)

    Sub MyOtherEventHandler(c As Boolean, newIntegerParameter As Integer, b As String) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_CustomEvents_ReferencedBy_RaiseEvent() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(x As Integer, y As String, z As Boolean)
    Custom Event $$MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(x As Integer, y As String, z As Boolean)
        End RaiseEvent
    End Event

    Sub M()
        RaiseEvent MyEvent(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, newIntegerParameter As Integer, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, newIntegerParameter As Integer, y As String)
        End RaiseEvent
    End Event

    Sub M()
        RaiseEvent MyEvent(True, 12345, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_CustomEvents_ReferencedBy_AddHandler() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(x As Integer, y As String, z As Boolean)
    Custom Event $$MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(x As Integer, y As String, z As Boolean)
        End RaiseEvent
    End Event

    Sub M()
        AddHandler MyEvent, AddressOf MyEventHandler
    End Sub

    Sub MyEventHandler(a As Integer, b As String, c As Boolean)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, newIntegerParameter As Integer, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, newIntegerParameter As Integer, y As String)
        End RaiseEvent
    End Event

    Sub M()
        AddHandler MyEvent, AddressOf MyEventHandler
    End Sub

    Sub MyEventHandler(c As Boolean, newIntegerParameter As Integer, b As String)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_CustomEvents_ReferencedBy_Invocations() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(x As Integer, y As String, z As Boolean)
    Custom Event $$MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(x As Integer, y As String, z As Boolean)
        End RaiseEvent
    End Event

    Sub M()
        Dim e As MyDelegate = Nothing
        e(1, "Two", True)
        e.Invoke(1, "Two", True)
        e.BeginInvoke(1, "Two", True, Nothing, New Object())
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, newIntegerParameter As Integer, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, newIntegerParameter As Integer, y As String)
        End RaiseEvent
    End Event

    Sub M()
        Dim e As MyDelegate = Nothing
        e(True, 12345, "Two")
        e.Invoke(True, 12345, "Two")
        e.BeginInvoke(True, 12345, "Two", Nothing, New Object())
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_CustomEvents_ReferencedBy_HandlesClause() As Task
            Dim markup = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(x As Integer, y As String, z As Boolean)
    Custom Event $$MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(x As Integer, y As String, z As Boolean)
        End RaiseEvent
    End Event

    Sub MyOtherEventHandler(a As Integer, b As String, c As Boolean) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(2),
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer"),
                New AddedParameterOrExistingIndex(1)}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, newIntegerParameter As Integer, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, newIntegerParameter As Integer, y As String)
        End RaiseEvent
    End Event

    Sub MyOtherEventHandler(c As Boolean, newIntegerParameter As Integer, b As String) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestAddParameter_Delegates_Generics() As Task
            Dim markup = <Text><![CDATA[
Delegate Sub $$MyDelegate(Of T)(t As T)

Class C
    Sub B()
        Dim d = New MyDelegate(Of Integer)(AddressOf M1)
    End Sub

    Sub M1(i As Integer)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {
                New AddedParameterOrExistingIndex(New AddedParameter(Nothing, "Integer", "newIntegerParameter", CallSiteKind.Value, "12345"), "Integer")}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MyDelegate(Of T)(newIntegerParameter As Integer)

Class C
    Sub B()
        Dim d = New MyDelegate(Of Integer)(AddressOf M1)
    End Sub

    Sub M1(newIntegerParameter As Integer)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Function
    End Class
End Namespace
