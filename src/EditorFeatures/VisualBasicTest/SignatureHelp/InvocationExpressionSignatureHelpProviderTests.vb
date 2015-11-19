' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class InvocationExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New InvocationExpressionSignatureHelpProvider()
        End Function

#Region "Regular tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutParameters()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        [|Foo($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo()", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(958593)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationInsideStringLiteral()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        [|Foo("$$"|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo()", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutParametersMethodXmlComments()
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Foo
    ''' </summary>
    Sub Foo()
        [|Foo($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo()", "Summary for Foo", Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersOn1()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo(a As Integer, b As Integer)
        [|Foo($$a, b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersXmlCommentsOn1()
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Foo
    ''' </summary>
    ''' <param name="a">Param a</param>
    ''' <param name="b">Param b</param>
    Sub Foo(a As Integer, b As Integer)
        [|Foo($$a, b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", "Summary for Foo", "Param a", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersOn2()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo(a As Integer, b As Integer)
        [|Foo(a, $$b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithParametersXmlCommentsOn2()
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Foo
    ''' </summary>
    ''' <param name="a">Param a</param>
    ''' <param name="b">Param b</param>
    Sub Foo(a As Integer, b As Integer)
        [|Foo(a, $$b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", "Summary for Foo", "Param b", currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutClosingParen()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        [|Foo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo()", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutClosingParenWithParametersOn1()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo(a As Integer, b As Integer)
        [|Foo($$a, b
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationWithoutClosingParenWithParametersOn2()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo(a As Integer, b As Integer)
        [|Foo(a, $$b
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseExpression_ProtectedAccessibility()
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Overridable Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|MyBase.Foo($$
    |]End Sub

    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseExpression_AbstractBase()
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected MustOverride Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|MyBase.Foo($$
    |]End Sub

    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnThisExpression_ProtectedAccessibility()
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Overridable Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Foo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnThisExpression_ProtectedAccessibility_Overridden()
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Overridable Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Foo($$
    |]End Sub
    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Derived.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase()
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected MustOverride Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Foo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase_Overridden()
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected MustOverride Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Foo($$
    |]End Sub
    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Derived.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseExpression_ProtectedFriendAccessibility()
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Friend Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|MyBase.Foo($$
    |]End Sub
    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseMember_ProtectedAccessibility_ThroughType()
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        Dim x as New Base()
        [|x.Foo($$
    |]End Sub
    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Test(markup, Nothing)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseExpression_PrivateAccessibility()
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Private Sub Foo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        Dim x as New Base()
        [|x.Foo($$
    |]End Sub
    Protected Overrides Sub Foo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Test(markup, Nothing)
        End Sub

        <WorkItem(968188)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseExpression_Constructor()
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected Sub New(x As Integer)
    End Sub
End Class
Public Class Derived
    Inherits Base
    Public Sub New()
        [|MyBase.New($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.New(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(968188), WorkItem(544989)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnBaseExpression_Finalizer()
            Dim markup = <a><![CDATA[
Class C
    Protected Overrides Sub Finalize()
        [|MyBase.Finalize($$
    |]End Sub
End Class]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Object.Finalize()"))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnSubLambda()
            Dim markup = <a><![CDATA[
Imports System

Class C
    Sub Foo(a As Integer, b As Integer)
        Dim bar As Action(Of Integer) = Sub(i) Console.WriteLine(i)
        [|bar($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Action(Of Integer)(obj As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnFunctionLambda()
            Dim markup = <a><![CDATA[
Imports System

Class C
    Sub Foo(a As Integer, b As Integer)
        Dim bar As Func(Of Integer, String) = Function(i) i.ToString()
        [|bar($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Func(Of Integer, String)(arg As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnLambdaInsideAnonType()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim lambda = Function() 0
        Dim bar = New With {.Value = [|lambda($$}
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Invoke() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnMemberAccessExpression()
            Dim markup = <a><![CDATA[
Class C
    Shared Sub Bar(a As Integer)
    End Sub

    Sub Foo()
        [|C.Bar($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Bar(a As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestExtensionMethod1()
            Dim markup = <a><![CDATA[
Imports System.Runtime.CompilerServices

Class C
    Sub Method()
        Dim s As String = "test"
        [|s.ExtensionMethod($$
    |]End Sub
End Class

Public Module MyExtension
    <Extension()>
    Public Function ExtensionMethod(s As String, x As Integer) As Integer
        Return s.Length
    End Function
End Module

]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{Extension}> MyExtension.ExtensionMethod(x As Integer) As Integer", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestProperty()
            Dim markup = <a><![CDATA[
Class C
    Property foo As Integer

    Sub bar()
        [|foo($$
    |]End Sub
End Class]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.foo() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(544068)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestExtension()
            Dim markup = <a><![CDATA[
Imports System.Runtime.CompilerServices
 
Public Class Foo
    Sub bar()
        Me.ExtensionMethod($$
    End Sub
End Class
 
Module SomeModule
    <Extension()>
    Public Sub ExtensionMethod(ByRef f As Foo)
 
    End Sub
End Module

]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem($"<{Extension}> SomeModule.ExtensionMethod()", String.Empty, Nothing, currentParameterIndex:=0)
            }

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnAnonymousType()
            Dim markup = <a><![CDATA[
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim x = New With {.A = 0, .B = 1}
        M(x).Add($$
    End Sub

    Function M(Of T)(foo As T) As List(Of T)
    End Function
End Module]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
$"List(Of 'a).Add(item As 'a)

{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} New With {{ .A As Integer, .B As Integer }}",
                                     String.Empty,
                                     String.Empty,
                                     currentParameterIndex:=0,
                                     description:=$"

{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} New With {{ .A As Integer, .B As Integer }}"))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(545118)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestStatic1()
            Dim markup = <a><![CDATA[
Class C
    Shared Sub Foo()
        Bar($$
    End Sub

    Shared Sub Bar()

    End Sub

    Sub Bar(i As Integer)

    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem("C.Bar()", currentParameterIndex:=0)
            }

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(545118)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestStatic2()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Bar($$
    End Sub

    Shared Sub Bar()

    End Sub

    Sub Bar(i As Integer)

    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem("C.Bar()", currentParameterIndex:=0),
                New SignatureHelpTestItem("C.Bar(i As Integer)", currentParameterIndex:=0)
            }

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(539111)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestFilteringInOverloadedGenericMethods()
            Dim markup = <a><![CDATA[
Class C
    Sub M()
        GenericMethod(Of String, Integer)(Nothing, 42$$)
    End Sub
    
    ''' <summary>
    ''' Hello Generic World!
    ''' </summary>
    ''' <typeparam name="T1">Type Param 1</typeparam>
    ''' <param name="i">Param 1 of type T1</param>
    ''' <returns>Null</returns>
    Function GenericMethod(Of T1)(i As T1) As C
        Return Nothing
    End Function

    ''' <summary>
    ''' Hello Generic World 2.0!
    ''' </summary>
    ''' <typeparam name="T1">Type Param 1</typeparam>
    ''' <typeparam name="T2">Type Param 2</typeparam>
    ''' <param name="i">Param 1 of type T1</param>
    ''' <param name="i2">Param 2 of type T2</param>
    ''' <returns>Null</returns>
    Function GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C
        Return Nothing
    End Function
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(
                New SignatureHelpTestItem(signature:="C.GenericMethod(Of String, Integer)(i As String, i2 As Integer) As C",
                                          methodDocumentation:="Hello Generic World 2.0!",
                                          parameterDocumentation:="Param 2 of type T2",
                                          currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub AwaitableItem()
            Dim markup = <a><![CDATA[
Imports System.Threading.Tasks

Class C
    ''' &lt;summary&gt;
    ''' Doc Comment!
    ''' &lt;/summary&gt;
    Async Function Foo() As Task
        Me.Foo($$
    End Function
End Class
]]></a>.Value

            Dim documentation = StringFromLines("", WorkspacesResources.Usage, "  Await Foo()")

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem("C.Foo() As Task", currentParameterIndex:=0, methodDocumentation:=documentation)
            }

            TestSignatureHelpWithMscorlib45(markup, expectedOrderedItems, LanguageNames.VisualBasic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub AwaitableItem2()
            Dim markup = <a><![CDATA[
Imports System.Threading.Tasks

Class C
    ''' &lt;summary&gt;
    ''' Doc Comment!
    ''' &lt;/summary&gt;
    Async Function Foo() As Task(Of Integer)
        Me.Foo($$
    End Function
End Class
]]></a>.Value

            Dim documentation = StringFromLines("", WorkspacesResources.Usage, "  Dim r as Integer = Await Foo()")

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem("C.Foo() As Task(Of Integer)", currentParameterIndex:=0, methodDocumentation:=documentation)
            }

            TestSignatureHelpWithMscorlib45(markup, expectedOrderedItems, LanguageNames.VisualBasic)
        End Sub

#End Region

#Region "Default Properties"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestDefaultProperty()
            Dim markup = <a><![CDATA[
Class C
    Default Public Property item(index As Integer) As String
        Get
            Return "foo"
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class

Class D
    Sub Foo
        Dim obj As New C
        [|obj($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(index As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

#End Region

#Region "Current Parameter Name"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestCurrentParameterName()

            Dim markup = <a><![CDATA[
Class C
    Sub Foo(someParameter As Integer, something As Boolean)
        Foo(something:=false, someParameter:=$$)
    End Sub
End Class
]]></a>.Value

            VerifyCurrentParameterName(markup, "someParameter")
        End Sub

#End Region

#Region "Trigger tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnTriggerParens()

            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        [|Foo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo()", String.Empty, Nothing, currentParameterIndex:=0))

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationOnTriggerComma()

            Dim markup = <a><![CDATA[
Class C
    Sub Foo(a As Integer, b As Integer)
        [|Foo(a,$$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestNoInvocationOnSpace()

            Dim markup = <a><![CDATA[
Class C
    Sub Foo(a As Integer, b As Integer)
        [|Foo(a, $$|]
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()

            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub ConditionalIndexing()
            Dim markup = <a><![CDATA[
Class C
    Dim x as String = Nothing

    Sub Foo()
        x?($$)
    End Sub
End Class
]]></a>.Value

            Dim expected = New SignatureHelpTestItem("String(index As Integer) As Char", currentParameterIndex:=0)

            Test(markup, {expected}, experimental:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub ConditionalMethodCall()
            Dim markup = <a><![CDATA[
Class C
    Dim x as String = Nothing

    Sub Foo()
        x?.ToString($$)
    End Sub
End Class
]]></a>.Value

            Dim expected = {New SignatureHelpTestItem("String.ToString() As String", currentParameterIndex:=0),
                            New SignatureHelpTestItem("String.ToString(provider As System.IFormatProvider) As String", currentParameterIndex:=0)}

            Test(markup, expected, experimental:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub ConditionalDelegateInvocation()
            Dim markup = <a><![CDATA[
Imports System
Class C
    Sub Foo()
        Dim x As Func(Of Integer, Integer)
        x?($$
    End Sub
End Class
]]></a>.Value

            Dim expected = {New SignatureHelpTestItem("Func(Of Integer, Integer)(arg As Integer) As Integer", currentParameterIndex:=0)}

            Test(markup, expected, experimental:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub NonIdentifierConditionalIndexer()
            Dim expected = {New SignatureHelpTestItem("String(index As Integer) As Char")}

            ' inline with a string literal
            Test("
Class C
    Sub M()
        Dim c = """"?($$
    End Sub
End Class
", expected)

            ' parenthesized expression
            Test("
Class C
    Sub M()
        Dim c = ("""")?($$
    End Sub
End Class
", expected)

            ' new object expression
            Test("
Class C
    Sub M()
        Dim c = (New System.String("" ""c, 1))?($$
    End Sub
End Class
", expected)

            ' more complicated parenthesized expression
            Test("
Class C
    Sub M()
        Dim c = (CType(Nothing, System.Collections.Generic.List(Of Integer)))?($$
    End Sub
End Class
", {New SignatureHelpTestItem("System.Collections.Generic.List(Of Integer)(index As Integer) As Integer")})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TriggerCharacterInComment01()
            Dim markup = "
Class C
    Sub M(p As String)
        M(',$$
    End Sub
End Class
"
            Test(markup, Enumerable.Empty(Of SignatureHelpTestItem)(), usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TriggerCharacterInString01()
            Dim markup = "
Class C
    Sub M(p As String)
        M("",$$""
    End Sub
End Class
"
            Test(markup, Enumerable.Empty(Of SignatureHelpTestItem)(), usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestTriggerCharacters()
            Dim expectedTriggerCharacters() As Char = {","c, "("c}
            Dim unexpectedTriggerCharacters() As Char = {" "c, "["c, "<"c}

            VerifyTriggerCharacters(expectedTriggerCharacters, unexpectedTriggerCharacters)
        End Sub

#End Region

#Region "EditorBrowsable tests"

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Foo.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Foo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Foo.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Foo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim f As Foo
        f.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Foo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=True)

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=False)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim f As Foo
        f.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("Foo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("Foo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("Foo.Bar(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_Method_Overloads_BothBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        New Foo().Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Foo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=New List(Of SignatureHelpTestItem),
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OverriddenSymbolsFilteredFromSigHelp()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim dd as D
        dd.Foo($$
    End Sub   
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    Public Overridable Sub Foo(original As Integer) 
    End Sub
End Class

Public Class D 
    Inherits B
    Public Overrides Sub Foo(derived As Integer) 
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("D.Foo(derived As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As C
        x.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class C
    Public Sub Foo() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Foo()", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As D
        x.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class B
    Public Sub Foo() 
    End Sub
End Class

Public Class D 
    Inherits B
    Public Overloads Sub Foo(x As Integer)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem) From {
                New SignatureHelpTestItem("B.Foo()", String.Empty, Nothing, currentParameterIndex:=0),
                New SignatureHelpTestItem("D.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0)
            }

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_HidingWithDifferentParameterList()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As D
        x.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    Public Sub Foo() 
    End Sub
End Class

Public Class D 
    Inherits B
    Public Sub Foo(x As Integer)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("D.Foo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()

            Dim markup = <Text><![CDATA[
Class Program 
    Inherits B
    Sub M()
        Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("B.Foo()", String.Empty, Nothing, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    Public Sub Foo(t As T)
    End Sub
    Public Sub Foo(i As Integer)  
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Foo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(t As T)  
    End Sub
    Public Sub Foo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(Of Integer).Foo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Foo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    Public Sub Foo(t As T)
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(i As Integer)  
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(Of Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Foo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(t As T)  
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Foo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii As C(Of Integer, Integer)
        cii.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    Public Sub Foo(t As T)
    End Sub  
    Public Sub Foo(u As U)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii As C(Of Integer, Integer)
        cii.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(t As T)
    End Sub
    Public Sub Foo(u As U)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii as C(Of Integer, Integer)
        cii.Foo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(t As T)
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Foo(u As U)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Foo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_DefaultProperty_BrowsableStateAlways()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Default Public Property Prop1(x As Integer) As String
        Get
            Return "Hi"
        End Get
        Set(value As String)
        End Set
    End Property
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(x As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_DefaultProperty_BrowsableStateNever()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Default Public Property Prop1(x As Integer) As String
        Get
            Return "Hi"
        End Get
        Set(value As String)
        End Set
    End Property
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(x As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_DefaultProperty_BrowsableStateAdvanced()

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cc As C
        cc($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
    Default Public Property Prop1(x As Integer) As String
        Get
            Return "Hi"
        End Get
        Set(value As String)
        End Set
    End Property
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(x As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=True)

            TestSignatureHelpInEditorBrowsableContexts(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=False)
        End Sub


#End Region

        <WorkItem(543038)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub SignatureHelpWhenALambdaExpressionDeclaredAndInvokedAtTheSameTime()
            Dim markup = <text>
Class C
    Private ReadOnly field As Integer
    Sub New
        field = Function(ByVal arg As Integer)
                     Return 2
                 End Function($$
    End Sub
End Class
</text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Invoke(arg As Integer) As Integer", currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub MethodUnavailableInOneLinkedFile()
            Dim markup = <text><![CDATA[<Workspace>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="FOO=true">
                                 <Document FilePath="SourceDocument">
class C
#if FOO
    sub bar()
    end sub
#endif
    sub foo()
        bar($$
    end sub
end class

                         </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                             </Project>
                         </Workspace>]]></text>.Value.NormalizeLineEndings()
            Dim expectedDescription = New SignatureHelpTestItem("C.bar()" + vbCrLf + vbCrLf + String.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available) + vbCrLf + String.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable) + vbCrLf + vbCrLf + FeaturesResources.UseTheNavigationBarToSwitchContext, currentParameterIndex:=0)
            VerifyItemWithReferenceWorker(markup, {expectedDescription}, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub ExcludeLinkedFilesWithInactiveRegions()
            Dim markup = <text><![CDATA[<Workspace>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="FOO=true,BAR=true">
                                 <Document FilePath="SourceDocument">
class C
#if FOO
    sub bar()
    end sub
#endif

#if BAR
    sub foo()
        bar($$
    end sub
#endif
                         </Document>
                                         </Project>
                             <Project Language = "Visual Basic" CommonReferences="true" AssemblyName="Proj2">
                                 <Document IsLinkFile = "true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                             </Project>
                             <Project Language = "Visual Basic" CommonReferences="true" AssemblyName="Proj3" PreprocessorSymbols="BAR=true">
                                 <Document IsLinkFile = "true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                             </Project>
                         </Workspace>]]></text>.Value.NormalizeLineEndings()

            Dim expectedDescription = New SignatureHelpTestItem("C.bar()" + $"\r\n\r\n{String.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{String.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}".Replace("\r\n", vbCrLf), currentParameterIndex:=0)
            VerifyItemWithReferenceWorker(markup, {expectedDescription}, False)
        End Sub

        <WorkItem(699, "https://github.com/dotnet/roslyn/issues/699")>
        <WorkItem(1068424)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericParameters1()
            Dim markup = <a><![CDATA[
Class C
    Sub M()
        Foo(""$$)
    End Sub

    Sub Foo(Of T)(a As T)
    End Sub

    Sub Foo(Of T, U)(a As T, b As U)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem) From
            {
                New SignatureHelpTestItem("C.Foo(Of String)(a As String)", String.Empty, String.Empty, currentParameterIndex:=0),
                New SignatureHelpTestItem("C.Foo(Of T, U)(a As T, b As U)", String.Empty)
            }

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(699, "https://github.com/dotnet/roslyn/issues/699")>
        <WorkItem(1068424)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericParameters2()
            Dim markup = <a><![CDATA[
Class C
    Sub M()
        Foo("", $$)
    End Sub

    Sub Foo(Of T)(a As T)
    End Sub

    Sub Foo(Of T, U)(a As T, b As U)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem) From
            {
                New SignatureHelpTestItem("C.Foo(Of T)(a As T)", String.Empty),
                New SignatureHelpTestItem("C.Foo(Of T, U)(a As T, b As U)", String.Empty, String.Empty, currentParameterIndex:=1)
            }

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(3537, "https://github.com/dotnet/roslyn/issues/3537")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestEscapedIdentifiers()
            Dim markup = "
Class C
    Sub [Next]()
        Dim x As New C
        x.Next($$)
    End Sub
End Class
"
            Test(markup, SpecializedCollections.SingletonEnumerable(New SignatureHelpTestItem("C.Next()", String.Empty)))
        End Sub

        <WorkItem(4144, "https://github.com/dotnet/roslyn/issues/4144")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpIsVisibleOnInaccessibleItem()
            Dim markup = "
Imports System.Collections.Generic

Class A
    Dim args As List(Of Integer)
End Class

Class B
    Inherits A

    Sub M()
        args.Add($$
    End Sub
End Class
"

            Test(markup, {New SignatureHelpTestItem("List(Of Integer).Add(item As Integer)")})
        End Sub
    End Class
End Namespace
