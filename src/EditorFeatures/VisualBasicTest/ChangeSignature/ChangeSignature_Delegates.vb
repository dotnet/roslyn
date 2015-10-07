' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ChangeSignature

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New ChangeSignatureCodeRefactoringProvider()
        End Function

        Protected Overrides Function CreateWorkspaceFromFile(definition As String, parseOptions As ParseOptions, compilationOptions As CompilationOptions) As TestWorkspace
            Return VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(definition, DirectCast(parseOptions, VisualBasicParseOptions), DirectCast(compilationOptions, VisualBasicCompilationOptions))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_ImplicitInvokeCalls()
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s(True, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_ExplicitInvokeCalls()
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.Invoke(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.Invoke(True, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_BeginInvokeCalls()
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.BeginInvoke(1, "Two", True, Nothing, Nothing)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing
        s.BeginInvoke(True, "Two", Nothing, Nothing)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_SubLambdas()
            Dim markup = <Text><![CDATA[
Delegate Sub $$MySub(x As Integer, y As String, z As Boolean)

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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, y As String)

Class C
    Sub M()
        Dim s As MySub = Nothing

        s = Sub()
            End Sub

        s = Sub(c As Boolean, b As String)
            End Sub

        s = Sub(c As Boolean, b As String) System.Console.WriteLine("Test")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_FunctionLambdas()
            Dim markup = <Text><![CDATA[
Delegate Function $$MyFunc(x As Integer, y As String, z As Boolean) As Integer

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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, y As String) As Integer

Class C
    Sub M()
        Dim f As MyFunc = Nothing

        f = Function()
                Return 1
            End Function

        f = Function(c As Boolean, b As String)
                Return 1
            End Function

        f = Function(c As Boolean, b As String) 1
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_ReferencingLambdas_MethodArgument()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, y As String) As Integer

Class C
    Sub M(f As MyFunc)
        M(Function(c As Boolean, b As String) 1)

        M(Function(c As Boolean, b As String)
              Return 1
          End Function)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_ReferencingLambdas_ReturnValue()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, y As String)

Class C
    Function M1() As MySub
        Return Sub(c As Boolean, b As String)
               End Sub
    End Function

    Function M2() As MySub
        Return Sub(c As Boolean, b As String) System.Console.WriteLine("Test")
    End Function
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_Recursive()
            Dim markup = <Text><![CDATA[
Delegate Function $$MyFunc(x As Integer, y As String, z As Boolean) As MyFunc

Class C
    Sub M()
        Dim f As MyFunc = Nothing
        f(1, "Two", True)(1, "Two", True)(1, "Two", True)(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, y As String) As MyFunc

Class C
    Sub M()
        Dim f As MyFunc = Nothing
        f(True, "Two")(True, "Two")(True, "Two")(True, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_DocComments()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
''' <summary>
'''   This is <see cref="MyFunc"/>, which has these methods:
'''     <see cref="MyFunc.New(Object, System.IntPtr)"/>
'''     <see cref="MyFunc.Invoke(Boolean, String)"/>
'''     <see cref="MyFunc.EndInvoke(System.IAsyncResult)"/>
'''     <see cref="MyFunc.BeginInvoke(Boolean, String, System.AsyncCallback, Object)"/>
''' </summary>
''' <param name="z">z!</param>
''' <param name="y">y!</param>
''' 
Delegate Sub MyFunc(z As Boolean, y As String)

Class C
    Sub M()
        Dim f As MyFunc = AddressOf Test
    End Sub

    ''' <param name="c"></param>
    ''' <param name="b"></param>
    ''' 
    Private Sub Test(c As Boolean, b As String)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_Relaxation_FunctionToSub()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MySub(z As Boolean, y As String)

Class C
    Sub M()
        Dim f As MySub = AddressOf Test
    End Sub

    Private Function Test(c As Boolean, b As String) As Integer
        Return 1
    End Function
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_Relaxation_ParameterlessFunctionToFunction()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Function MyFunc(z As Boolean, y As String) As Integer

Class C
    Sub M()
        Dim f As MyFunc = AddressOf Test
    End Sub

    Private Function Test()
        Return 1
    End Function
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_CascadeToEvents()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, y As String)

    Event MyEvent As MyDelegate
    Custom Event MyEvent2 As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, y As String)
        End RaiseEvent
    End Event

    Sub M()
        RaiseEvent MyEvent(True, "Two")
        RaiseEvent MyEvent2(True, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Events_ReferencedBy_RaiseEvent()
            Dim markup = <Text><![CDATA[
Class C
    Event $$MyEvent(x As Integer, y As String, z As Boolean)

    Sub M()
        RaiseEvent MyEvent(1, "Two", True)
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, y As String)

    Sub M()
        RaiseEvent MyEvent(True, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Events_ReferencedBy_AddHandler()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, y As String)

    Sub M()
        AddHandler MyEvent, AddressOf MyEventHandler
    End Sub

    Sub MyEventHandler(c As Boolean, b As String)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Events_ReferencedBy_GeneratedDelegateTypeInvocations()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, y As String)

    Sub M()
        Dim e As MyEventEventHandler = Nothing
        e(True, "Two")
        e.Invoke(True, "Two")
        e.BeginInvoke(True, "Two", Nothing, New Object())
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Events_ReferencedBy_HandlesClause()
            Dim markup = <Text><![CDATA[
Class C
    Event $$MyEvent(x As Integer, y As String, z As Boolean)

    Sub MyOtherEventHandler(a As Integer, b As String, c As Boolean) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Event MyEvent(z As Boolean, y As String)

    Sub MyOtherEventHandler(c As Boolean, b As String) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_CustomEvents_ReferencedBy_RaiseEvent()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, y As String)
        End RaiseEvent
    End Event

    Sub M()
        RaiseEvent MyEvent(True, "Two")
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_CustomEvents_ReferencedBy_AddHandler()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, y As String)
        End RaiseEvent
    End Event

    Sub M()
        AddHandler MyEvent, AddressOf MyEventHandler
    End Sub

    Sub MyEventHandler(c As Boolean, b As String)
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_CustomEvents_ReferencedBy_Invocations()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, y As String)
        End RaiseEvent
    End Event

    Sub M()
        Dim e As MyDelegate = Nothing
        e(True, "Two")
        e.Invoke(True, "Two")
        e.BeginInvoke(True, "Two", Nothing, New Object())
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_CustomEvents_ReferencedBy_HandlesClause()
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
            Dim updatedSignature = {2, 1}
            Dim expectedUpdatedCode = <Text><![CDATA[
Class C
    Delegate Sub MyDelegate(z As Boolean, y As String)
    Custom Event MyEvent As MyDelegate
        AddHandler(value As MyDelegate)
        End AddHandler
        RemoveHandler(value As MyDelegate)
        End RemoveHandler
        RaiseEvent(z As Boolean, y As String)
        End RaiseEvent
    End Event

    Sub MyOtherEventHandler(c As Boolean, b As String) Handles Me.MyEvent
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Sub ChangeSignature_Delegates_Generics()
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
            Dim updatedSignature = Array.Empty(Of Integer)()
            Dim expectedUpdatedCode = <Text><![CDATA[
Delegate Sub MyDelegate(Of T)()

Class C
    Sub B()
        Dim d = New MyDelegate(Of Integer)(AddressOf M1)
    End Sub

    Sub M1()
    End Sub
End Class
]]></Text>.NormalizedValue()
            TestChangeSignatureViaCommand(LanguageNames.VisualBasic, markup, updatedSignature:=updatedSignature, expectedUpdatedInvocationDocumentCode:=expectedUpdatedCode)
        End Sub
    End Class
End Namespace
