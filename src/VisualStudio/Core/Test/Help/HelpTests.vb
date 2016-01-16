' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Help
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Help
    Public Class HelpTests
        Public Async Function TestAsync(markup As String, expected As String) As Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(markup)
                Dim caret = workspace.Documents.First().CursorPosition
                Dim service = New VisualBasicHelpContextService()
                Assert.Equal(expected, Await service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None))
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAddHandler1() As Task
            Dim text = <a>
Class G
    Public Event MyEvent()

    Public Sub G()
        AddH[||]andler MyEvent, AddressOf G
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.AddHandler")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAddHandler2() As Task
            Dim text = <a>
Class G
    Public Event MyEvent()

    Public Sub G()
        AddHandler MyEvent,[||] AddressOf G
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.AddHandler")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestArrayInitializer() As Task
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() = new Integer() {1,[||] 2, 3}
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.Array")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestArrayInitializer2() As Task
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() = new[||] Integer() {1, 2, 3}
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.Array")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAssignment1() As Task
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() =[||] new {1, 2, 3}
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.=")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAttribute() As Task
            Dim text = <a><![CDATA[
Class GAttribute
            Inherits System.Attribute

    <G>[||]
    Public Sub G()
                Dim x As Integer() =[||] New {1, 2, 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Attributes)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestModuleAttribute() As Task
            Dim text = <a><![CDATA[
Imports System.Reflection
<Assembly: AssemblyTitleAttribute("Production assembly 4"), Mod[||]ule: CLSCompliant(True)>
Module M

End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.ModuleAttribute)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAssemblyAttribute() As Task
            Dim text = <a><![CDATA[
Imports System.Reflection
<Ass[||]embly: AssemblyTitleAttribute("Production assembly 4"), Module: CLSCompliant(True)>
Module M

End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.AssemblyAttribute)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestBinaryOperator() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x = 2 +[||] 3
    End Sub
ENd Class]]></a>

            Await TestAsync(text.Value, "vb.+")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCallStatement() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        C[||]all G()
    End Sub
ENd Class]]></a>

            Await TestAsync(text.Value, "vb.Call")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCase1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = 2 + 3
        Select Case x
            Ca[||]se 1
                G()
            Case Else
                x = 3
        End Select
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Select")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCase2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = 2 + 3
        Select Case x
            Case 1
                G()
            Case E[||]lse
                x = 3
        End Select
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Select")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTryCatch() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Try G()

        Catch ex As[||] Exception When 2 = 2

        End Try
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.As")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTryCatch2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Try G()

        Catch ex As Exception W[||]hen 2 = 2

        End Try
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.When")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTryCatch3() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Try G()

        Catch ex As Exception When 2 = 2

        [|Finally|]

        End Try
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Try")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCollectionInitializer() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x As List(Of Integer)
        x = New List(Of Integer) Fr[||]om {1, 2, 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.CollectionInitializer)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCollectionInitializer2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x As List(Of Integer)
        x = New List(Of Integer) From {1,[||] 2, 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.CollectionInitializer)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestConstructor() As Task
            Dim text = <a><![CDATA[
Class G
    Sub Ne[||]w()
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Constructor)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDistinct() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim customerOrders = From cust In {1, 2, 3}, ord In {1, 2, 3}
                     Where cust= ord
                     Select cust.CompanyName
                     Dist[||]inct
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.QueryDistinct)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDoLoop() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Do

        Loop Un[||]til False
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDoLoop2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Do

        Loop Un[||]til False
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDoLoop3() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Do[||]

        Loop Until False
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Do")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIfThenElse1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        If True Then

        ElseIf False The[||]n

        End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Then")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIfThenElse2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        If True Then

        ElseI[||]f False Then

        Else

        End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.ElseIf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIfThenElse3() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        If True Then

        ElseIf False Then

        Els[||]e

        End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Else")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIfThenElse4() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        I[||]f True Then

        ElseIf False Then

        End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEndFunctionLambda() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x as Function(Of Integer) = Function()
                                            return 2
                                        End Functi[||]on
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.LambdaFunction)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEndBlockKind() As Task
            Dim text = <a><![CDATA[
Class G
En[||]d Class]]></a>

            Await TestAsync(text.Value, "vb.Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEndAddHandler() As Task
            Dim text = <a><![CDATA[
Class G
        Public Custom Event e As EventHandler
            AddHandler(value As EventHandler)

            End AddH[||]andler
            RemoveHandler(value As EventHandler)

            End RemoveHandler
            RaiseEvent(sender As Object, e As EventArgs)

            End RaiseEvent
        End Event
    End Class]]></a>

            Await TestAsync(text.Value, "vb.AddHandler")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEndSub() As Task
            Dim text = <a><![CDATA[
Class G
    Sub foo()
        End[||]
    ENd Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.End")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEnumMember() As Task
            Dim text = <a><![CDATA[
Enum G
    A[||]
End Enum]]></a>

            Await TestAsync(text.Value, "vb.Enum")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEraseStatement() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x(9, 9), y(9, 9) as Integer
        Erase[||] x, y
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Erase")
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEraseStatement2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x(9, 9), y(9, 9) as Integer
        Erase x[|,|] y
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Erase")
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestError() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Er[||]ror 1
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Error")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestEvent() As Task
            Dim text = <a><![CDATA[
Class G
    Ev[||]ent e As EventHandler
End Class]]></a>

            Await TestAsync(text.Value, "vb.Event")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestExit1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub Foo()
        While True
            Exit [|While|]
        End While
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestExit2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub Foo()
        While True
            Exit While
        End While
        Exit [|Sub|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestField1() As Task
            Dim text = <a><![CDATA[
Class G
    Protec[||]ted foo as Integer
End Class]]></a>

            Await TestAsync(text.Value, "vb.Protected")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestField2() As Task
            Dim text = <a><![CDATA[
Class G
    Protected ReadOn[||]ly foo as Integer
End Class]]></a>

            Await TestAsync(text.Value, "vb.ReadOnly")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestField3() As Task
            Dim text = <a><![CDATA[
Class G
    [|Dim|] foo as Integer
End Class]]></a>

            Await TestAsync(text.Value, "vb.Dim")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestForEach() As Task
            Dim text = <a><![CDATA[
Class G
    For each x [|in|] {0}
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.In")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestForEach2() As Task
            Dim text = <a><![CDATA[
Class G
    For [|each|] x in {0}
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.Each")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestForEach3() As Task
            Dim text = <a><![CDATA[
Class G
    [|For|] each x in {0}
    NExt
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.ForEach)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestFor() As Task
            Dim text = <a><![CDATA[
Class G
    For x = 1 to 3 [|Step|] 2
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.Step")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestFor2() As Task
            Dim text = <a><![CDATA[
Class G
    For x = 1 [|to|] 3 Step 2
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestFor3() As Task
            Dim text = <a><![CDATA[
Class G
    [|Fo|]r x = 1 to 3 Step 2
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.For")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestFrom() As Task
            Dim text = <a><![CDATA[
Class G
    Dim z = F[||]rom x in {1 2 3} select x
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.QueryFrom)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTypeParameter1() As Task
            Dim text = <a><![CDATA[
Interface I(Of [|Out|] R)
    Function Do() as R
End Interface]]></a>

            Await TestAsync(text.Value, HelpKeywords.VarianceOut)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestGetType() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = GetT[|y|]pe(G)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.GetType")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestGoTo() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
    label:
        Goto [|label|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.GoTo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestLabel() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
    [|label:|]
        Goto [|label|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Colon)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIfOperator() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = [|If|](true, 0, 1)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.IfOperator)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestImplements1() As Task
            Dim text = <a><![CDATA[
Interface IFoo 
End Interface
Interface IBar
End Interface
Class G
    [|Implements|] IFoo, Ibar
End Class]]></a>

            Await TestAsync(text.Value, "vb.Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestImplements2() As Task
            Dim text = <a><![CDATA[
Interface IFoo 
End Interface
Interface IBar
End Interface
Class G
    Implements IFoo[|,|] IBar
End Class]]></a>

            Await TestAsync(text.Value, "vb.Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousType1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = [|New|] With {Key .Foo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousType2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New [|With|] {Key .Foo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousType3() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {[|Key|] .Foo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousKey)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousType4() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {Key [|.Foo|] = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestJoinOn() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {Key [|.Foo|] = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTypeOf1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = 1
        Dim y = [|TypeOf|] x is Integer
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.TypeOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTypeOf2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = 1
        Dim y = TypeOf x [|i|]s Integer
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.TypeOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestLambda1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 As Func(Of Task(Of Integer)) = [|Async|] Function()
                                                  Return Await Task.FromResult(2)
                                              End Function

    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Async")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestLambda2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 As Func(Of Task(Of Integer)) = Async F[||]unction()
                                                  Return Await Task.FromResult(2)
                                              End Function

    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.LambdaFunction)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestLetClause() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim q = from x in {1, 2, 3}
                [|let|] z = x
                select x

    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.QueryLet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPartialMethod() As Task
            Dim text = <a><![CDATA[
Class G
    [|Partial|] Sub Foo()
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PartialMethod)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestMainMethod() As Task
            Dim text = <a><![CDATA[
Module Foo
    Sub m[||]ain()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.Main)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestMeToken() As Task
            Dim text = <a><![CDATA[
Module Foo
    Sub main()
        [|Me|].main()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.Me")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestConstructRatherThanName() As Task
            Dim text = <a><![CDATA[
Module [|Foo|]
    Sub main()
        main()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestMyBase() As Task
            Dim text = <a><![CDATA[
Class Foo
    Sub main()
        My[|Base|].GetType()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.MyBase")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestMyClass() As Task
            Dim text = <a><![CDATA[
Class Foo
    Sub main()
        My[|Base|].GetType()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.MyBase")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNewConstraint() As Task
            Dim text = <a><![CDATA[
Interface IBar
End Interface
Class Foo(Of T As {IBar, [|New|]})
    Sub main()
        MyBase.GetType()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.NewConstraint)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestObjectInitializer() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Property foo As Integer
    Sub fooo()
        Dim p = New Program [|With|] {.foo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.ObjectInitializer)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNothingToken() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Property foo As Integer
    Sub fooo()
        Dim p = New Program [|With|] {.foo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.ObjectInitializer)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNullable1() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim [|p?|] as boolean
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Nullable)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOnError() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        On Error Resume [|Next|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OnError)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOptionCompare() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Compare [|Binary|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionCompare)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOptionExplicit() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Explicit [|Off|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionExplicit)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOptionInfer() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Infer [|Off|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionInfer)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOptionStrict() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Strict [|Off|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionStrict)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOption() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        [|Option|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Option")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPredefinedCast() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim x = [|CInt|](1)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.CInt")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorConst() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #const x [|=|] 3
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorConst)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorConditional1() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #[|If|] true Then
        #ElseIF Flase Then
        #Else
        #End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorConditional2() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #If true Then
        #[|ElseIf|] Flase Then
        #Else
        #End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorConditional3() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #If true Then
        #ElseIf Flase Then
        #[|Else|]
        #End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorConditional4() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #If true Then
        #ElseIf Flase Then
        #Else
        #[|End|] If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorRegion1() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #[|Region|]
        #End Region
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Region)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPreprocessorRegion2() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #Region
        [|#End|] Region
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Region)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestRaiseEvent() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Event e as EventHandler
    Sub fooo()
        RaiseEve[||]nt e(nothing, nothing)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.RaiseEvent")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestReDim() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim arr(10, 10) as Integer
        ReDim [|Preserve|] array(10, 30)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Redim)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIsOperator() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim a, b as Object
        DIm c = a [|Is|] b
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Is")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestRemoveHandler() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Event e As EventHandler
    Public Sub EHandler(sender As Object, e As EventArgs)

    End Sub
    Sub fooo()
        Re[||]moveHandler e, AddressOf EHandler
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.RemoveHandler")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestResume() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Resume [|Next|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Resume")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestReturn() As Task
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        [|Return|] 3
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Return")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestStop() As Task
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        St[||]op
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Stop")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestSyncLock() As Task
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        DIm lock = new Object()
        Syn[||]cLock lock
        End SyncLock
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.SyncLock")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestThrow() As Task
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        [|Throw|] New System.Exception()
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Throw")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNegate() As Task
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        Dim x = 3
        y = [|-|]x
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Negate)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestUsingStatement() As Task
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        Dim x as IDisposable = nothing
        Us[||]ing x
        End Using
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Using")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestYieldStatement() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        [|Yield|] 1
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "vb.Yield")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestLocalDeclaration()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        [|Dim|] x = 3
    End Function
End Class]]></a>
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPredefinedType() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        Dim x as [|Integer|]
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "vb.Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestIdentifierName() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        System.Console.Wri[||]teLine(2)
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "System.Console.WriteLine")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDateLiteral() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        Dim x = #5/30/19[||]90#
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "vb.Date")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDocComment() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    ''' <summary>
    ''' [||]
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())

    End Sub
End Module]]></a>.Value, HelpKeywords.XmlDocComment)
        End Function

        <WorkItem(864194)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousType() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim mm = Sub(ByRef x As String, y As Integer) System.Console.WriteLine(), k[||]k = Sub(y, x) mm(y, x)
    End Sub
End Class]]></a>.Value, "vb.AnonymousType")
        End Function

        <WorkItem(864189)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousProperty() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim query = From iii In {1, 2, 3}
                    Select New With {.P[||]1 = iii}
        Dim i = query.First().P1

    End Sub
End Module]]></a>.Value, "vb.AnonymousType")
        End Function

        <WorkItem(863684)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestByVal() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(ByV[||]al args As String())

    End Sub
End Module]]></a>.Value, "vb.ByVal")
        End Function

        <WorkItem(864207)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOf() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main([||]Of T)(args As String())

    End Sub
End Module]]></a>.Value, "vb.Of")
        End Function

        <WorkItem(863680)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCompoundAssign() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim i = 0
        i [||]+= 1
        i -= 2
        i *= 3
        i /= 4
    End Sub
End Class
]]></a>.Value, "vb.+=")
        End Function

        <WorkItem(863661)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestGeneric() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As System.Collections.Generic[||].IEnumerable(Of Integer)

    End Sub
End Module]]></a>.Value, "System.Collections.Generic.IEnumerable`1")
        End Function

        <WorkItem(863652)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestSub() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        
    End S[||]ub
End Module]]></a>.Value, "vb.Sub")
        End Function

        <WorkItem(863340)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAsNew() As Task
            Await TestAsync(<a><![CDATA[Imports System.Text
Public Class Test
    Sub Subroutine()
        Dim sb A[||]s New StringBuilder
    End Sub
End Class
]]></a>.Value, "vb.As")


        End Function

        <WorkItem(863305)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAwait() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Async Sub AsyncSub()
        Dim x2 = Async Function() As Task(Of Integer)
                     Return A[||]wait AsyncFuncNG(10)
                 End Function
    End Sub
End Class
]]></a>.Value, "vb.Await")
        End Function

        <WorkItem(864243)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestProperty() As Task
            Await TestAsync(<a><![CDATA[Class Program
    Prope[||]rty prop As Integer
End Class]]></a>.Value, "vb.AutoImplementedProperty")
        End Function

        <WorkItem(864226)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPredefinedTypeMember() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x = Integer.MaxVa[||]lue
    End Sub
End Module]]></a>.Value, "System.Int32.MaxValue")
        End Function

        <WorkItem(864237)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestModuleModifier() As Task
            Await TestAsync(<a><![CDATA[Publi[||]c Module M
    Public Class C
        Protected Sub S1()
        End Sub
    End Class
    Private Interface I
    End Interface
    Friend ReadOnly Property Prop As String
        Get
        End Get
    End Property
End Module
Public Delegate Sub Dele()
]]></a>.Value, "vb.Public")
        End Function

        <WorkItem(864237)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestDelegateModifiers() As Task
            Await TestAsync(<a><![CDATA[Public Module M
    Public Class C
        Protected Sub S1()
        End Sub
    End Class
    Private Interface I
    End Interface
    Friend ReadOnly Property Prop As String
        Get
        End Get
    End Property
End Module
Publi[||]c Delegate Sub Dele()
]]></a>.Value, "vb.Public")
        End Function

        <WorkItem(863273)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAssignment() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim x =[||] Int32.Parse("1")
    End Sub
End Class
]]></a>.Value, "vb.=")
        End Function

        <WorkItem(863228)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestRem() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        ' COmm[||]ent!
    End Sub
End Module]]></a>.Value, "vb.Rem")
        End Function

        <WorkItem(863228)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTodo() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        ' TODO: COmm[||]ent!
    End Sub
End Module]]></a>.Value, HelpKeywords.TaskListUserComments)
        End Function

        <WorkItem(863220)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestMethodInvocation() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Sub Subroutine()
    End Sub
    Sub AnotherSub()
        Subroutine()[||]
    End Sub
End Class
]]></a>.Value, "vb.Call")
        End Function

        <WorkItem(864202)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestImportsXmlns() As Task
            Await TestAsync(<a><![CDATA[Imports <xmln[||]s:ns="foo">]]></a>.Value, "vb.ImportsXmlns")
        End Function

        <WorkItem(862420)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestParameter() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(a[||]rgs As String())
        
    End Sub
End Module]]></a>.Value, "System.String()")
        End Function

        <WorkItem(862396)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNoToken() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
[||]
    End Sub
End Module]]></a>.Value, "")
        End Function

        <WorkItem(863293)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestMemberAccess() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Int32.[||]Parse("1")
    End Sub
End Module]]></a>.Value, "System.Int32.Parse")
        End Function

        <WorkItem(864661)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCtype2() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

        Dim local5 = If(CTy[||]pe(3, Object), Nothing)
    End Sub
End Module]]></a>.Value, "vb.CType")
        End Function

        <WorkItem(864661)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNothing() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

        Dim local5 = If(CType(3, Object), Noth[||]ing)
    End Sub
End Module]]></a>.Value, "vb.Nothing")
        End Function

        <WorkItem(864658)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNullable() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim Value1a As Integer?[||] = 10
    End Sub
End Module]]></a>.Value, "vb.Nullable")
        End Function

        <WorkItem(864209)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestRegionTrivia() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

#Region "mor[||]e"
#End Region

    End Sub
End Module]]></a>.Value, "vb.String")
        End Function

        <WorkItem(865034)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestTypeCharacter() As Task
            Await TestAsync(<a><![CDATA[Public Module M
    Sub M1()
        Dim u = 1[||]UI
        Dim ul = &HBADC0DE
        Dim l = -1L
    End Sub
End Module]]></a>.Value, "vb.UInteger")
        End Function

        <WorkItem(865061)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestStructure() As Task
            Await TestAsync(<a><![CDATA[Structure S[||]1
End Structure
]]></a>.Value, "vb.Structure")
        End Function

        <WorkItem(865047)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestXmlLiteralDocument() As Task
            Await TestAsync(<a><![CDATA[Public Module M
    Sub M1()
        Dim MyXMLLiteral = <?xml versio[||]n="1.0" encoding="utf-8"?>
                           <Details>

                           </Details>

        Dim y = <!-- -->
        Dim z = <e/>

    End Sub
End Module
]]></a>.Value, "vb.XmlLiteralDocument")
        End Function

        <WorkItem(865047)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestXmlEmptyElement() As Task
            Await TestAsync(<a><![CDATA[Public Module M
    Sub M1()
        Dim MyXMLLiteral = <?xml version="1.0" encoding="utf-8"?>
                           <Details>

                           </Details>

        Dim y = <!-- -->
        Dim z = <e[||]/>

    End Sub
End Module
]]></a>.Value, "vb.XmlLiteralElement")
        End Function

        <WorkItem(865047)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestXmlLiteralComment() As Task
            Await TestAsync(<a><![CDATA[Public Module M
    Sub M1()
        Dim MyXMLLiteral = <?xml version="1.0" encoding="utf-8"?>
                           <Details>

                           </Details>

        Dim y = <!--[||] -->
        Dim z = <e/>

    End Sub
End Module
]]></a>.Value, "vb.XmlLiteralComment")
        End Function

        <WorkItem(865088)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestWhile() As Task
            Await TestAsync(<a><![CDATA[Class C
    Sub M()
        Dim icount = 0
        Wh[||]ile icount <= 100
            icount += 1
        End While

    End Sub
End Class]]></a>.Value, "vb.While")
        End Function

        <WorkItem(865326)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestImplements() As Task
            Await TestAsync(<a><![CDATA[Interface I1
Sub M()
End Interface
Class C
Implements I1
Public Sub M() Imple[||]ments I1.M
End Sub
End Class
]]></a>.Value, "vb.ImplementsClause")
        End Function

        <WorkItem(865306)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAddressOf() As Task
            Await TestAsync(<a><![CDATA[Delegate Sub mydele()
Class C
Sub M1()
End Sub
Sub M()
Dim d1 As New mydele(Addre[||]ssOf M1)
Dim addr As mydele = AddressOf M1
End Sub
End Class
]]></a>.Value, "vb.AddressOf")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestShared() As Task
            Await TestAsync(<a><![CDATA[[|Shared|]]]></a>.Value, "vb.Shared")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestWidening() As Task
            Await TestAsync(<a><![CDATA[[|Widening|]]]></a>.Value, "vb.Widening")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCType() As Task
            Await TestAsync(<a><![CDATA[[|CType|]]]></a>.Value, "vb.CType")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNarrowing() As Task
            Await TestAsync(<a><![CDATA[[|Narrowing|]]]></a>.Value, "vb.Narrowing")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOperator() As Task
            Await TestAsync(<a><![CDATA[[|Operator|]]]></a>.Value, "vb.Operator")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAddHandler() As Task
            Await TestAsync(<a><![CDATA[[|AddHandler|]]]></a>.Value, "vb.AddHandler")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnsi() As Task
            Await TestAsync(<a><![CDATA[Declare [|Ansi|]]]></a>.Value, "vb.Ansi")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAuto() As Task
            Await TestAsync(<a><![CDATA[Declare [|Auto|]]]></a>.Value, "vb.Auto")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestUnicode() As Task
            Await TestAsync(<a><![CDATA[Declare [|Unicode|]]]></a>.Value, "vb.Unicode")
        End Function

        <WorkItem(898157)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestHandles() As Task
            Await TestAsync(<a><![CDATA[[|Handles|]]]></a>.Value, "vb.Handles")
        End Function

        <WorkItem(867738)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestImplementsIDisposable() As Task
            Await TestAsync(<a><![CDATA[Imports System
Class C
    Implements IDis[||]posable
    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Class
]]></a>.Value, "vb.IDisposable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestInherits() As Task
            Await TestAsync(<a><![CDATA[Imports System
Class C
    Inherits Exc[||]eption

End Class
]]></a>.Value, "System.Exception")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNot() As Task
            Await TestAsync(<a><![CDATA[Class C
    Sub M()
        Dim b = False
        b = N[||]ot b
    End Sub
End Class]]></a>.Value, "vb.Not")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestArrayIndex() As Task
            Await TestAsync(<a><![CDATA[Class C
    Sub M()
        Dim a(4) As Integer
        a[||](0) = 1
    End Sub
End Class]]></a>.Value, "vb.Integer")
        End Function


        <WorkItem(866074)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestGroupJoin() As Task
            Await TestAsync(<a><![CDATA[Module LINQ
    Sub Linq()
        Dim customers As New List(Of Customer)()
        customers.Add(New Customer() With {.ID = 1, .Address = "shanghai"})
        customers.Add(New Customer() With {.ID = 2, .Address = "beijing"})
        Dim query1 = From c In customers
                     Let d = c
                     Where d IsNot Nothing
                     Group Jo[||]in c1 In customers On d.Address.GetHashCode() Equals c1.Address.GetHashCode() Into e = Group
                     Group c By c.Address Into g = Group
                     Order By g.Count() Ascending
                     Order By Address Descending
                     Select New With {Key .Address = Address, Key .CustCount = g.Count()}
    End Sub
    Class Customer
        Public Property ID() As Integer
        Public Property Address() As String
    End Class
End Module]]></a>.Value, "vb.QueryGroupJoin")
        End Function

        <WorkItem(866074)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestGroupJoinIn() As Task
            Await TestAsync(<a><![CDATA[Module LINQ
    Sub Linq()
        Dim customers As New List(Of Customer)()
        customers.Add(New Customer() With {.ID = 1, .Address = "shanghai"})
        customers.Add(New Customer() With {.ID = 2, .Address = "beijing"})
        Dim query1 = From c In customers
                     Let d = c
                     Where d IsNot Nothing
                     Group Join c1 I[||]n customers On d.Address.GetHashCode() Equals c1.Address.GetHashCode() Into e = Group
                     Group c By c.Address Into g = Group
                     Order By g.Count() Ascending
                     Order By Address Descending
                     Select New With {Key .Address = Address, Key .CustCount = g.Count()}
    End Sub
    Class Customer
        Public Property ID() As Integer
        Public Property Address() As String
    End Class
End Module]]></a>.Value, "vb.QueryGroupJoinIn")
        End Function

        <WorkItem(866074)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestLinqEquals() As Task
            Await TestAsync(<a><![CDATA[Module LINQ
    Sub Linq()
        Dim customers As New List(Of Customer)()
        customers.Add(New Customer() With {.ID = 1, .Address = "shanghai"})
        customers.Add(New Customer() With {.ID = 2, .Address = "beijing"})
        Dim query1 = From c In customers
                     Let d = c
                     Where d IsNot Nothing
                     Group Join c1 In customers On d.Address.GetHashCode() Equ[||]als c1.Address.GetHashCode() Into e = Group
                     Group c By c.Address Into g = Group
                     Order By g.Count() Ascending
                     Order By Address Descending
                     Select New With {Key .Address = Address, Key .CustCount = g.Count()}
    End Sub
    Class Customer
        Public Property ID() As Integer
        Public Property Address() As String
    End Class
End Module]]></a>.Value, "vb.Equals")
        End Function

        <WorkItem(866074)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestSelect() As Task
            Await TestAsync(<a><![CDATA[Module LINQ
    Sub Linq()
        Dim customers As New List(Of Customer)()
        customers.Add(New Customer() With {.ID = 1, .Address = "shanghai"})
        customers.Add(New Customer() With {.ID = 2, .Address = "beijing"})
        Dim query1 = From c In customers
                     Let d = c
                     Where d IsNot Nothing
                     Group Join c1 In customers On d.Address.GetHashCode() Equals c1.Address.GetHashCode() Into e = Group
                     Group c By c.Address Into g = Group
                     Order By g.Count() Ascending
                     Order By Address Descending
                     Sele[||]ct New With {Key .Address = Address, Key .CustCount = g.Count()}
    End Sub
    Class Customer
        Public Property ID() As Integer
        Public Property Address() As String
    End Class
End Module]]></a>.Value, "vb.QuerySelect")
        End Function

        <WorkItem(866074)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestCountLinq() As Task
            Await TestAsync(<a><![CDATA[Module LINQ
    Sub Linq()
        Dim customers As New List(Of Customer)()a
        customers.Add(New Customer() With {.ID = 1, .Address = "shanghai"})
        customers.Add(New Customer() With {.ID = 2, .Address = "beijing"})
        Dim query1 = From c In customers
                     Let d = c
                     Where d IsNot Nothing
                     Group Join c1 In customers On d.Address.GetHashCode() Equals c1.Address.GetHashCode() Into e = Group
                     Group c By c.Address Into g = Group
                     Order By g.Count() Ascending
                     Order By Address Descending
                     Select New With {Key .Address = Address, Key .CustCount = g.Coun[||]t()}
    End Sub
    Class Customer
        Public Property ID() As Integer
        Public Property Address() As String
    End Class
End Module]]></a>.Value, "System.Linq.Enumerable.Count")
        End Function

        <WorkItem(867747)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestOperatorOverload() As Task
            Await TestAsync(<a><![CDATA[Class C
    Public Shared Operator IsTr[||]ue(ByVal a As C) As Boolean
        Return False
    End Operator
End Class]]></a>.Value, "vb.IsTrue")
        End Function

        <WorkItem(866058)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAnonymousLocal() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim produc[||]tList = {New With {.category = "Condiments", .name = "Ketchup"}, New With {.category = "Seafood", .name = "Code"}}
    End Sub
End Module]]></a>.Value, "vb.AnonymousType")
        End Function

        <WorkItem(866046)>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestNoEscaping() As Task
            Await TestAsync(<a><![CDATA[Imports System
Class C
    Sub M()
        Dim x = "hello"
        Dim t = x.Get[||]Type
    End Sub
End Class]]></a>.Value, "System.Object.GetType")
        End Function

        <WorkItem(4150, "https://github.com/dotnet/roslyn/issues/4150")>
        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestPropertyFromMemberAccess() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        args.Le[||]ngth
    End Sub
End Module]]></a>.Value, "System.Array.Length")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestParameterFromReference() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        a[||]rgs
    End Sub
End Module]]></a>.Value, "System.String()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestLocalFromReference() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x As Integer
        x[||]
    End Sub
End Module]]></a>.Value, "System.Int32")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestAliasFromReference() As Task
            Await TestAsync(<a><![CDATA[Imports s = System.Linq.Enumerable

Module Program
    Sub Main(args As String())
        Dim x As s[||]
    End Sub
End Module]]></a>.Value, "System.Linq.Enumerable")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Async Function TestRangeVariable() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim z = From x In args Select x[||]
    End Sub
End Module]]></a>.Value, "vb.String")
        End Function
    End Class
End Namespace

