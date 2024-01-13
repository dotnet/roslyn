' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class InvocationExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(InvocationExpressionSignatureHelpProvider)
        End Function

#Region "Regular tests"

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithoutParameters() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        [|Goo($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25830")>
        Public Async Function PickCorrectOverload_PickString() As Task

            Dim markup = <Text><![CDATA[
Public Class C
    Sub M()
        [|M(i:="Hello"$$|])
    End Sub

    Public Sub M(i As String)
    End Sub
    Public Sub M(i As Integer)
    End Sub
    Public Sub M(filtered As Byte)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = {
                New SignatureHelpTestItem("C.M(i As Integer)", String.Empty, Nothing, currentParameterIndex:=0),
                New SignatureHelpTestItem("C.M(i As String)", String.Empty, Nothing, currentParameterIndex:=0, isSelected:=True)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25830")>
        Public Async Function PickCorrectOverload_PickInteger() As Task

            Dim markup = <Text><![CDATA[
Public Class C
    Sub M()
        [|M(i:=1$$|])
    End Sub

    Public Sub M(i As String)
    End Sub
    Public Sub M(i As Integer)
    End Sub
    Public Sub M(filtered As Byte)
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = {
                New SignatureHelpTestItem("C.M(i As Integer)", String.Empty, Nothing, currentParameterIndex:=0, isSelected:=True),
                New SignatureHelpTestItem("C.M(i As String)", String.Empty, Nothing, currentParameterIndex:=0)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958593")>
        Public Async Function TestInvocationInsideStringLiteral() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        [|Goo("$$"|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo()", currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithoutParametersMethodXmlComments() As Task
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Goo
    ''' </summary>
    Sub Goo()
        [|Goo($$|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo()", "Summary for Goo", Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithParametersOn1() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo(a As Integer, b As Integer)
        [|Goo($$a, b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithParametersXmlCommentsOn1() As Task
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Goo
    ''' </summary>
    ''' <param name="a">Param a</param>
    ''' <param name="b">Param b</param>
    Sub Goo(a As Integer, b As Integer)
        [|Goo($$a, b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", "Summary for Goo", "Param a", currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithParametersOn2() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo(a As Integer, b As Integer)
        [|Goo(a, $$b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithParametersXmlCommentsOn2() As Task
            Dim markup = <a><![CDATA[
Class C
    ''' <summary>
    ''' Summary for Goo
    ''' </summary>
    ''' <param name="a">Param a</param>
    ''' <param name="b">Param b</param>
    Sub Goo(a As Integer, b As Integer)
        [|Goo(a, $$b|])
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", "Summary for Goo", "Param b", currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithoutClosingParen() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        [|Goo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithoutClosingParenWithParametersOn1() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo(a As Integer, b As Integer)
        [|Goo($$a, b
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationWithoutClosingParenWithParametersOn2() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo(a As Integer, b As Integer)
        [|Goo(a, $$b
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnBaseExpression_ProtectedAccessibility() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Overridable Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|MyBase.Goo($$
    |]End Sub

    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnBaseExpression_AbstractBase() As Task
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected MustOverride Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|MyBase.Goo($$
    |]End Sub

    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnThisExpression_ProtectedAccessibility() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Overridable Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Goo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnThisExpression_ProtectedAccessibility_Overridden() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Overridable Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Goo($$
    |]End Sub
    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Derived.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase() As Task
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected MustOverride Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Goo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnThisExpression_ProtectedAccessibility_AbstractBase_Overridden() As Task
            Dim markup = <a><![CDATA[
Imports System
Public MustInherit Class Base
    Protected MustOverride Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|Me.Goo($$
    |]End Sub
    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Derived.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnBaseExpression_ProtectedFriendAccessibility() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Friend Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        [|MyBase.Goo($$
    |]End Sub
    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Base.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnBaseMember_ProtectedAccessibility_ThroughType() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Protected Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        Dim x as New Base()
        [|x.Goo($$
    |]End Sub
    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Await TestAsync(markup, Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnBaseExpression_PrivateAccessibility() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class Base
    Private Sub Goo(x As Integer)
    End Sub
End Class

Public Class Derived
    Inherits Base
    Private Sub Test()
        Dim x as New Base()
        [|x.Goo($$
    |]End Sub
    Protected Overrides Sub Goo(x As Integer)
    End Sub
End Class
]]></a>.Value

            Await TestAsync(markup, Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188")>
        Public Async Function TestInvocationOnBaseExpression_Constructor() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968188"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544989")>
        Public Async Function TestInvocationOnBaseExpression_Finalizer() As Task
            Dim markup = <a><![CDATA[
Class C
    Protected Overrides Sub Finalize()
        [|MyBase.Finalize($$
    |]End Sub
End Class]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Object.Finalize()"))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnSubLambda() As Task
            Dim markup = <a><![CDATA[
Imports System

Class C
    Sub Goo(a As Integer, b As Integer)
        Dim bar As Action(Of Integer) = Sub(i) Console.WriteLine(i)
        [|bar($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Action(Of Integer)(obj As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnFunctionLambda() As Task
            Dim markup = <a><![CDATA[
Imports System

Class C
    Sub Goo(a As Integer, b As Integer)
        Dim bar As Func(Of Integer, String) = Function(i) i.ToString()
        [|bar($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Func(Of Integer, String)(arg As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnLambdaInsideAnonType() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim lambda = Function() 0
        Dim bar = New With {.Value = [|lambda($$}
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Invoke() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnMemberAccessExpression() As Task
            Dim markup = <a><![CDATA[
Class C
    Shared Sub Bar(a As Integer)
    End Sub

    Sub Goo()
        [|C.Bar($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Bar(a As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestExtensionMethod1() As Task
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
            expectedOrderedItems.Add(New SignatureHelpTestItem($"<{VBFeaturesResources.Extension}> MyExtension.ExtensionMethod(x As Integer) As Integer", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestProperty() As Task
            Dim markup = <a><![CDATA[
Class C
    Property goo As Integer

    Sub bar()
        [|goo($$
    |]End Sub
End Class]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.goo() As Integer", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544068")>
        Public Async Function TestExtension() As Task
            Dim markup = <a><![CDATA[
Imports System.Runtime.CompilerServices
 
Public Class Goo
    Sub bar()
        Me.ExtensionMethod($$
    End Sub
End Class
 
Module SomeModule
    <Extension()>
    Public Sub ExtensionMethod(ByRef f As Goo)
 
    End Sub
End Module

]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem($"<{VBFeaturesResources.Extension}> SomeModule.ExtensionMethod()", String.Empty, Nothing, currentParameterIndex:=0)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnAnonymousType() As Task
            Dim markup = <a><![CDATA[
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim x = New With {.A = 0, .B = 1}
        M(x).Add($$
    End Sub

    Function M(Of T)(goo As T) As List(Of T)
    End Function
End Module]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
$"List(Of 'a).Add(item As 'a)

{FeaturesResources.Types_colon}
    'a {FeaturesResources.is_} New With {{ .A As Integer, .B As Integer }}",
                                     String.Empty,
                                     String.Empty,
                                     currentParameterIndex:=0,
                                     description:=$"

{FeaturesResources.Types_colon}
    'a {FeaturesResources.is_} New With {{ .A As Integer, .B As Integer }}"))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")>
        Public Async Function TestStatic1() As Task
            Dim markup = <a><![CDATA[
Class C
    Shared Sub Goo()
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545118")>
        Public Async Function TestStatic2() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539111")>
        Public Async Function TestFilteringInOverloadedGenericMethods() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestAwaitableItem() As Task
            Dim markup = <a><![CDATA[
Imports System.Threading.Tasks

Class C
    ''' &lt;summary&gt;
    ''' Doc Comment!
    ''' &lt;/summary&gt;
    Async Function Goo() As Task
        Me.Goo($$
    End Function
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem("C.Goo() As Task", currentParameterIndex:=0, methodDocumentation:=String.Empty)
            }

            Await TestSignatureHelpWithMscorlib45Async(markup, expectedOrderedItems, LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestAwaitableItem2() As Task
            Dim markup = <a><![CDATA[
Imports System.Threading.Tasks

Class C
    ''' &lt;summary&gt;
    ''' Doc Comment!
    ''' &lt;/summary&gt;
    Async Function Goo() As Task(Of Integer)
        Me.Goo($$
    End Function
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)() From {
                New SignatureHelpTestItem("C.Goo() As Task(Of Integer)", currentParameterIndex:=0, methodDocumentation:=String.Empty)
            }

            Await TestSignatureHelpWithMscorlib45Async(markup, expectedOrderedItems, LanguageNames.VisualBasic)
        End Function

#End Region

#Region "Default Properties"

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestDefaultProperty() As Task
            Dim markup = <a><![CDATA[
Class C
    Default Public Property item(index As Integer) As String
        Get
            Return "goo"
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class

Class D
    Sub Goo
        Dim obj As New C
        [|obj($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(index As Integer) As String", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

#End Region

#Region "Current Parameter Name"

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestCurrentParameterName() As Task

            Dim markup = <a><![CDATA[
Class C
    Sub Goo(someParameter As Integer, something As Boolean)
        Goo(something:=false, someParameter:=$$)
    End Sub
End Class
]]></a>.Value

            Await VerifyCurrentParameterNameAsync(markup, "someParameter")
        End Function

#End Region

#Region "Trigger tests"

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnTriggerParens() As Task

            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        [|Goo($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationOnTriggerComma() As Task

            Dim markup = <a><![CDATA[
Class C
    Sub Goo(a As Integer, b As Integer)
        [|Goo(a,$$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo(a As Integer, b As Integer)", String.Empty, String.Empty, currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestNoInvocationOnSpace() As Task

            Dim markup = <a><![CDATA[
Class C
    Sub Goo(a As Integer, b As Integer)
        [|Goo(a, $$|]
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()

            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestConditionalIndexing() As Task
            Dim markup = <a><![CDATA[
Class C
    Dim x as String = Nothing

    Sub Goo()
        x?($$)
    End Sub
End Class
]]></a>.Value

            Dim expected = New SignatureHelpTestItem("String(index As Integer) As Char", currentParameterIndex:=0)

            Await TestAsync(markup, {expected}, experimental:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestConditionalMethodCall() As Task
            Dim markup = <a><![CDATA[
Class C
    Dim x as String = Nothing

    Sub Goo()
        x?.ToString($$)
    End Sub
End Class
]]></a>.Value

            Dim expected = {New SignatureHelpTestItem("String.ToString() As String", currentParameterIndex:=0),
                            New SignatureHelpTestItem("String.ToString(provider As System.IFormatProvider) As String", currentParameterIndex:=0)}

            Await TestAsync(markup, expected, experimental:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestConditionalDelegateInvocation() As Task
            Dim markup = <a><![CDATA[
Imports System
Class C
    Sub Goo()
        Dim x As Func(Of Integer, Integer)
        x?($$
    End Sub
End Class
]]></a>.Value

            Dim expected = {New SignatureHelpTestItem("Func(Of Integer, Integer)(arg As Integer) As Integer", currentParameterIndex:=0)}

            Await TestAsync(markup, expected, experimental:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestNonIdentifierConditionalIndexer() As Task
            Dim expected = {New SignatureHelpTestItem("String(index As Integer) As Char")}

            ' inline with a string literal
            Await TestAsync("
Class C
    Sub M()
        Dim c = """"?($$
    End Sub
End Class
", expected)

            ' parenthesized expression
            Await TestAsync("
Class C
    Sub M()
        Dim c = ("""")?($$
    End Sub
End Class
", expected)

            ' new object expression
            Await TestAsync("
Class C
    Sub M()
        Dim c = (New System.String("" ""c, 1))?($$
    End Sub
End Class
", expected)

            ' more complicated parenthesized expression
            Await TestAsync("
Class C
    Sub M()
        Dim c = (CType(Nothing, System.Collections.Generic.List(Of Integer)))?($$
    End Sub
End Class
", {New SignatureHelpTestItem("System.Collections.Generic.List(Of Integer)(index As Integer) As Integer")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestTriggerCharacterInComment01() As Task
            Dim markup = "
Class C
    Sub M(p As String)
        M(',$$
    End Sub
End Class
"
            Await TestAsync(markup, Enumerable.Empty(Of SignatureHelpTestItem)(), usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestTriggerCharacterInString01() As Task
            Dim markup = "
Class C
    Sub M(p As String)
        M("",$$""
    End Sub
End Class
"
            Await TestAsync(markup, Enumerable.Empty(Of SignatureHelpTestItem)(), usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestTriggerCharacters()
            Dim expectedTriggerCharacters() As Char = {","c, "("c}
            Dim unexpectedTriggerCharacters() As Char = {" "c, "["c, "<"c}

            VerifyTriggerCharacters(expectedTriggerCharacters, unexpectedTriggerCharacters)
        End Sub

#End Region

#Region "EditorBrowsable tests"

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_Method_BrowsableStateAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Goo.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Goo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_Method_BrowsableStateNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Goo.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared Sub Bar() 
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Goo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_Method_BrowsableStateAdvanced() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim f As Goo
        f.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Sub Bar() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("Goo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=True)

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim f As Goo
        f.Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)>
    Public Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("Goo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("Goo.Bar()", String.Empty, Nothing, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("Goo.Bar(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_Method_Overloads_BothBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        New Goo().Bar($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class Goo
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Bar() 
    End Sub

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Bar(x As Integer) 
    End Sub
End Class
]]></Text>.Value

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=New List(Of SignatureHelpTestItem),
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestOverriddenSymbolsFilteredFromSigHelp() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim dd as D
        dd.Goo($$
    End Sub   
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    Public Overridable Sub Goo(original As Integer) 
    End Sub
End Class

Public Class D 
    Inherits B
    Public Overrides Sub Goo(derived As Integer) 
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("D.Goo(derived As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As C
        x.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class C
    Public Sub Goo() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C.Goo()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As D
        x.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
Public Class B
    Public Sub Goo() 
    End Sub
End Class

Public Class D 
    Inherits B
    Public Overloads Sub Goo(x As Integer)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem) From {
                New SignatureHelpTestItem("B.Goo()", String.Empty, Nothing, currentParameterIndex:=0),
                New SignatureHelpTestItem("D.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0)
            }

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_HidingWithDifferentParameterList() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim x As D
        x.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    Public Sub Goo() 
    End Sub
End Class

Public Class D 
    Inherits B
    Public Sub Goo(x As Integer)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("D.Goo(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_BrowsableStateNeverMethodsInBaseClass() As Task

            Dim markup = <Text><![CDATA[
Class Program 
    Inherits B
    Sub M()
        Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class B
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo() 
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("B.Goo()", String.Empty, Nothing, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    Public Sub Goo(t As T)
    End Sub
    Public Sub Goo(i As Integer)  
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Goo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)  
    End Sub
    Public Sub Goo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(Of Integer).Goo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Goo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    Public Sub Goo(t As T)
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(i As Integer)  
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(Of Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer).Goo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim ci = New C(Of Integer)()
        ci.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)  
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(i As Integer)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer).Goo(i As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii As C(Of Integer, Integer)
        cii.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    Public Sub Goo(t As T)
    End Sub  
    Public Sub Goo(u As U)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii As C(Of Integer, Integer)
        cii.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)
    End Sub
    Public Sub Goo(u As U)
    End Sub
End Class
]]></Text>.Value
            Dim expectedOrderedItemsMetadataReference = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsMetadataReference.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Dim expectedOrderedItemsSameSolution = New List(Of SignatureHelpTestItem)()
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItemsSameSolution.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItemsMetadataReference,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItemsSameSolution,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever() As Task

            Dim markup = <Text><![CDATA[
Class Program
    Sub M()
        Dim cii as C(Of Integer, Integer)
        cii.Goo($$
    End Sub
End Class
]]></Text>.Value

            Dim referencedCode = <Text><![CDATA[
Public Class C(Of T, U)
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(t As T)
    End Sub
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public Sub Goo(u As U)  
    End Sub
End Class
]]></Text>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(t As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem("C(Of Integer, Integer).Goo(u As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_DefaultProperty_BrowsableStateAlways() As Task

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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_DefaultProperty_BrowsableStateNever() As Task

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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(7336, "DevDiv_Projects/Roslyn")>
        Public Async Function TestEditorBrowsable_DefaultProperty_BrowsableStateAdvanced() As Task

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

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=New List(Of SignatureHelpTestItem)(),
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=True)

            Await TestSignatureHelpInEditorBrowsableContextsAsync(markup:=markup,
                                                referencedCode:=referencedCode,
                                                expectedOrderedItemsMetadataReference:=expectedOrderedItems,
                                                expectedOrderedItemsSameSolution:=expectedOrderedItems,
                                                sourceLanguage:=LanguageNames.VisualBasic,
                                                referencedLanguage:=LanguageNames.VisualBasic,
                                                hideAdvancedMembers:=False)
        End Function

#End Region

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543038")>
        Public Async Function TestSignatureHelpWhenALambdaExpressionDeclaredAndInvokedAtTheSameTime() As Task
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

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestMethodUnavailableInOneLinkedFile() As Task
            Dim markup = <text><![CDATA[<Workspace>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO=true">
                                 <Document FilePath="SourceDocument">
class C
#if GOO
    sub bar()
    end sub
#endif
    sub goo()
        bar($$
    end sub
end class

                         </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                             </Project>
                         </Workspace>]]></text>.Value.NormalizeLineEndings()
            Dim expectedDescription = New SignatureHelpTestItem("C.bar()" + vbCrLf + vbCrLf + String.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available) + vbCrLf + String.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available) + vbCrLf + vbCrLf + FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts, currentParameterIndex:=0)
            Await VerifyItemWithReferenceWorkerAsync(markup, {expectedDescription}, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestExcludeLinkedFilesWithInactiveRegions() As Task
            Dim markup = <text><![CDATA[<Workspace>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO=true,BAR=true">
                                 <Document FilePath="SourceDocument">
class C
#if GOO
    sub bar()
    end sub
#endif

#if BAR
    sub goo()
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

            Dim expectedDescription = New SignatureHelpTestItem("C.bar()" + $"\r\n\r\n{String.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{String.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}".Replace("\r\n", vbCrLf), currentParameterIndex:=0)
            Await VerifyItemWithReferenceWorkerAsync(markup, {expectedDescription}, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/699")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")>
        Public Async Function TestGenericParameters1() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub M()
        Goo(""$$)
    End Sub

    Sub Goo(Of T)(a As T)
    End Sub

    Sub Goo(Of T, U)(a As T, b As U)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem) From
            {
                New SignatureHelpTestItem("C.Goo(Of String)(a As String)", String.Empty, String.Empty, currentParameterIndex:=0),
                New SignatureHelpTestItem("C.Goo(Of T, U)(a As T, b As U)", String.Empty)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/699")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068424")>
        Public Async Function TestGenericParameters2() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub M()
        Goo("", $$)
    End Sub

    Sub Goo(Of T)(a As T)
    End Sub

    Sub Goo(Of T, U)(a As T, b As U)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem) From
            {
                New SignatureHelpTestItem("C.Goo(Of T)(a As T)", String.Empty),
                New SignatureHelpTestItem("C.Goo(Of T, U)(a As T, b As U)", String.Empty, String.Empty, currentParameterIndex:=1)
            }

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/3537")>
        Public Async Function TestEscapedIdentifiers() As Task
            Dim markup = "
Class C
    Sub [Next]()
        Dim x As New C
        x.Next($$)
    End Sub
End Class
"
            Await TestAsync(markup, SpecializedCollections.SingletonEnumerable(New SignatureHelpTestItem("C.Next()", String.Empty)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/4144")>
        Public Async Function TestSigHelpIsVisibleOnInaccessibleItem() As Task
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

            Await TestAsync(markup, {New SignatureHelpTestItem("List(Of Integer).Add(item As Integer)")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2579")>
        Public Async Function TestInvocationOnMeExpression_Constructor() As Task
            Dim markup = <a><![CDATA[
Imports System
Public Class A
    Public Sub New()
        [|Me.New($$
    |]End Sub
    Public Sub New(x As Integer)
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem("A.New(x As Integer)", String.Empty, String.Empty, currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/40451")>
        Public Async Function TestSigHelpIsVisibleWithDuplicateMethodNames() As Task
            Dim markup = "
Class C
    Shared Sub Test()
        M(1, 2$$)
    End Sub

    Sub M(y As Integer)
    End Sub

    Shared Sub M(x As Integer, y As Integer)
    End Sub
End Class
"

            Await TestAsync(markup, {New SignatureHelpTestItem("C.M(x As Integer, y As Integer)")})
        End Function
    End Class
End Namespace
