' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Help
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Help
    Public Class HelpTests
        Public Sub Test(markup As String, expected As String)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(markup)
                Dim caret = workspace.Documents.First().CursorPosition
                Dim service = New VisualBasicHelpContextService()
                Assert.Equal(expected, service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None).WaitAndGetResult(CancellationToken.None))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AddHandler1()
            Dim text = <a>
Class G
    Public Event MyEvent()

    Public Sub G()
        AddH[||]andler MyEvent, AddressOf G
    End Sub
End Class</a>

            Test(text.Value, "vb.AddHandler")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AddHandler2()
            Dim text = <a>
Class G
    Public Event MyEvent()

    Public Sub G()
        AddHandler MyEvent,[||] AddressOf G
    End Sub
End Class</a>

            Test(text.Value, "vb.AddHandler")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ArrayInitializer()
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() = new Integer() {1,[||] 2, 3}
    End Sub
End Class</a>

            Test(text.Value, "vb.Array")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ArrayInitializer2()
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() = new[||] Integer() {1, 2, 3}
    End Sub
End Class</a>

            Test(text.Value, "vb.Array")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Assignment()
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() =[||] new {1, 2, 3}
    End Sub
End Class</a>

            Test(text.Value, "vb.=")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Attribute()
            Dim text = <a><![CDATA[
Class GAttribute
            Inherits System.Attribute

    <G>[||]
    Public Sub G()
                Dim x As Integer() =[||] New {1, 2, 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Attributes)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ModuleAttribute()
            Dim text = <a><![CDATA[
Imports System.Reflection
<Assembly: AssemblyTitleAttribute("Production assembly 4"), Mod[||]ule: CLSCompliant(True)>
Module M

End Module]]></a>

            Test(text.Value, HelpKeywords.ModuleAttribute)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AssemblyAttribute()
            Dim text = <a><![CDATA[
Imports System.Reflection
<Ass[||]embly: AssemblyTitleAttribute("Production assembly 4"), Module: CLSCompliant(True)>
Module M

End Module]]></a>

            Test(text.Value, HelpKeywords.AssemblyAttribute)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub BinaryOperator()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x = 2 +[||] 3
    End Sub
ENd Class]]></a>

            Test(text.Value, "vb.+")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub CallStatement()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        C[||]all G()
    End Sub
ENd Class]]></a>

            Test(text.Value, "vb.Call")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Case1()
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

            Test(text.Value, "vb.Select")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Case2()
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

            Test(text.Value, "vb.Select")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TryCatch()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Try G()

        Catch ex As[||] Exception When 2 = 2

        End Try
    End Sub
End Class]]></a>

            Test(text.Value, "vb.As")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TryCatch2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Try G()

        Catch ex As Exception W[||]hen 2 = 2

        End Try
    End Sub
End Class]]></a>

            Test(text.Value, "vb.When")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TryCatch3()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Try G()

        Catch ex As Exception When 2 = 2

        [|Finally|]

        End Try
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Try")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub CollectionInitializer()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x As List(Of Integer)
        x = New List(Of Integer) Fr[||]om {1, 2, 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.CollectionInitializer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub CollectionInitializer2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x As List(Of Integer)
        x = New List(Of Integer) From {1,[||] 2, 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.CollectionInitializer)
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Constructor()
            Dim text = <a><![CDATA[
Class G
    Sub Ne[||]w()
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Constructor)
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Distinct()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim customerOrders = From cust In {1, 2, 3}, ord In {1, 2, 3}
                     Where cust= ord
                     Select cust.CompanyName
                     Dist[||]inct
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.QueryDistinct)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub DoLoop()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Do

        Loop Un[||]til False
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub DoLoop2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Do

        Loop Un[||]til False
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub DoLoop3()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Do[||]

        Loop Until False
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IfThenElse1()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        If True Then

        ElseIf False The[||]n

        End If
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Then")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IfThenElse2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        If True Then

        ElseI[||]f False Then

        Else

        End If
    End Sub
End Class]]></a>

            Test(text.Value, "vb.ElseIf")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IfThenElse3()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        If True Then

        ElseIf False Then

        Els[||]e

        End If
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Else")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IfThenElse4()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        I[||]f True Then

        ElseIf False Then

        End If
    End Sub
End Class]]></a>

            Test(text.Value, "vb.If")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EndFunctionLambda()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x as Function(Of Integer) = Function()
                                            return 2
                                        End Functi[||]on
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.LambdaFunction)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EndBlockKind()
            Dim text = <a><![CDATA[
Class G
En[||]d Class]]></a>

            Test(text.Value, "vb.Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EndAddHandler()
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

            Test(text.Value, "vb.AddHandler")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EndSub()
            Dim text = <a><![CDATA[
Class G
    Sub foo()
        End[||]
    ENd Sub
End Class]]></a>

            Test(text.Value, "vb.End")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EnumMember()
            Dim text = <a><![CDATA[
Enum G
    A[||]
End Enum]]></a>

            Test(text.Value, "vb.Enum")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EraseStatement()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x(9, 9), y(9, 9) as Integer
        Erase[||] x, y
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Erase")
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub EraseStatement2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x(9, 9), y(9, 9) as Integer
        Erase x[|,|] y
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Erase")
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Error]()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Er[||]ror 1
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Error")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Event]()
            Dim text = <a><![CDATA[
Class G
    Ev[||]ent e As EventHandler
End Class]]></a>

            Test(text.Value, "vb.Event")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Exit1()
            Dim text = <a><![CDATA[
Class G
    Sub Foo()
        While True
            Exit [|While|]
        End While
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Exit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Exit2()
            Dim text = <a><![CDATA[
Class G
    Sub Foo()
        While True
            Exit While
        End While
        Exit [|Sub|]
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Exit")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Field1()
            Dim text = <a><![CDATA[
Class G
    Protec[||]ted foo as Integer
End Class]]></a>

            Test(text.Value, "vb.Protected")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Field2()
            Dim text = <a><![CDATA[
Class G
    Protected ReadOn[||]ly foo as Integer
End Class]]></a>

            Test(text.Value, "vb.ReadOnly")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Field3()
            Dim text = <a><![CDATA[
Class G
    [|Dim|] foo as Integer
End Class]]></a>

            Test(text.Value, "vb.Dim")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ForEach()
            Dim text = <a><![CDATA[
Class G
    For each x [|in|] {0}
    NExt
End Class]]></a>

            Test(text.Value, "vb.In")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ForEach2()
            Dim text = <a><![CDATA[
Class G
    For [|each|] x in {0}
    NExt
End Class]]></a>

            Test(text.Value, "vb.Each")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ForEach3()
            Dim text = <a><![CDATA[
Class G
    [|For|] each x in {0}
    NExt
End Class]]></a>

            Test(text.Value, HelpKeywords.ForEach)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [For]()
            Dim text = <a><![CDATA[
Class G
    For x = 1 to 3 [|Step|] 2
    NExt
End Class]]></a>

            Test(text.Value, "vb.Step")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub For2()
            Dim text = <a><![CDATA[
Class G
    For x = 1 [|to|] 3 Step 2
    NExt
End Class]]></a>

            Test(text.Value, "vb.To")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub For3()
            Dim text = <a><![CDATA[
Class G
    [|Fo|]r x = 1 to 3 Step 2
    NExt
End Class]]></a>

            Test(text.Value, "vb.For")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [From]()
            Dim text = <a><![CDATA[
Class G
    Dim z = F[||]rom x in {1 2 3} select x
End Class]]></a>

            Test(text.Value, HelpKeywords.QueryFrom)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TypeParameter1()
            Dim text = <a><![CDATA[
Interface I(Of [|Out|] R)
    Function Do() as R
End Interface]]></a>

            Test(text.Value, HelpKeywords.VarianceOut)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestGetType()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = GetT[|y|]pe(G)
    End Sub
End Class]]></a>

            Test(text.Value, "vb.GetType")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [GoTo]()
            Dim text = <a><![CDATA[
Class G
    Sub G()
    label:
        Goto [|label|]
    End Sub
End Class]]></a>

            Test(text.Value, "vb.GoTo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Label()
            Dim text = <a><![CDATA[
Class G
    Sub G()
    [|label:|]
        Goto [|label|]
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Colon)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IfOperator()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = [|If|](true, 0, 1)
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.IfOperator)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Implements1()
            Dim text = <a><![CDATA[
Interface IFoo 
End Interface
Interface IBar
End Interface
Class G
    [|Implements|] IFoo, Ibar
End Class]]></a>

            Test(text.Value, "vb.Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Implements2()
            Dim text = <a><![CDATA[
Interface IFoo 
End Interface
Interface IBar
End Interface
Class G
    Implements IFoo[|,|] IBar
End Class]]></a>

            Test(text.Value, "vb.Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AnonymousType1()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = [|New|] With {Key .Foo = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.AnonymousType)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AnonymousType2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New [|With|] {Key .Foo = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.AnonymousType)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AnonymousType3()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {[|Key|] .Foo = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.AnonymousKey)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub AnonymousType4()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {Key [|.Foo|] = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.AnonymousType)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub JoinOn()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {Key [|.Foo|] = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.AnonymousType)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TypeOf1()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = 1
        Dim y = [|TypeOf|] x is Integer
    End Sub
End Class]]></a>

            Test(text.Value, "vb.TypeOf")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TypeOf2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = 1
        Dim y = TypeOf x [|i|]s Integer
    End Sub
End Class]]></a>

            Test(text.Value, "vb.TypeOf")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Lambda1()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 As Func(Of Task(Of Integer)) = [|Async|] Function()
                                                  Return Await Task.FromResult(2)
                                              End Function

    End Sub
End Class]]></a>

            Test(text.Value, "vb.Async")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Lambda2()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 As Func(Of Task(Of Integer)) = Async F[||]unction()
                                                  Return Await Task.FromResult(2)
                                              End Function

    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.LambdaFunction)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub LetClause()
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim q = from x in {1, 2, 3}
                [|let|] z = x
                select x

    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.QueryLet)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PartialMethod()
            Dim text = <a><![CDATA[
Class G
    [|Partial|] Sub Foo()
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.PartialMethod)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub MainMethod()
            Dim text = <a><![CDATA[
Module Foo
    Sub m[||]ain()
    End Sub
End Module]]></a>

            Test(text.Value, HelpKeywords.Main)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub MeToken()
            Dim text = <a><![CDATA[
Module Foo
    Sub main()
        [|Me|].main()
    End Sub
End Module]]></a>

            Test(text.Value, "vb.Me")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ConstructRatherThanName()
            Dim text = <a><![CDATA[
Module [|Foo|]
    Sub main()
        main()
    End Sub
End Module]]></a>

            Test(text.Value, "vb.Module")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [MyBase]()
            Dim text = <a><![CDATA[
Class Foo
    Sub main()
        My[|Base|].GetType()
    End Sub
End Module]]></a>

            Test(text.Value, "vb.MyBase")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [MyClass]()
            Dim text = <a><![CDATA[
Class Foo
    Sub main()
        My[|Base|].GetType()
    End Sub
End Module]]></a>

            Test(text.Value, "vb.MyBase")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub NewConstraint()
            Dim text = <a><![CDATA[
Interface IBar
End Interface
Class Foo(Of T As {IBar, [|New|]})
    Sub main()
        MyBase.GetType()
    End Sub
End Module]]></a>

            Test(text.Value, HelpKeywords.NewConstraint)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub ObjectInitializer()
            Dim text = <a><![CDATA[
Class Program
    Public Property foo As Integer
    Sub fooo()
        Dim p = New Program [|With|] {.foo = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.ObjectInitializer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub NothingToken()
            Dim text = <a><![CDATA[
Class Program
    Public Property foo As Integer
    Sub fooo()
        Dim p = New Program [|With|] {.foo = 3}
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.ObjectInitializer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Nullable()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim [|p?|] as boolean
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Nullable)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub OnError()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        On Error Resume [|Next|]
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.OnError)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub OptionCompare()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Compare [|Binary|]
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.OptionCompare)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub OptionExplicit()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Explicit [|Off|]
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.OptionExplicit)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub OptionInfer()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Infer [|Off|]
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.OptionInfer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub OptionStrict()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Option Strict [|Off|]
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.OptionStrict)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Option]()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        [|Option|]
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Option")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PredefinedCast()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim x = [|CInt|](1)
    End Sub
End Class]]></a>

            Test(text.Value, "vb.CInt")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorConst()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #const x [|=|] 3
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.PreprocessorConst)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorConditional1()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #[|If|] true Then
        #ElseIF Flase Then
        #Else
        #End If
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.PreprocessorIf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorConditional2()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #If true Then
        #[|ElseIf|] Flase Then
        #Else
        #End If
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.PreprocessorIf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorConditional3()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #If true Then
        #ElseIf Flase Then
        #[|Else|]
        #End If
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.PreprocessorIf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorConditional4()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #If true Then
        #ElseIf Flase Then
        #Else
        #[|End|] If
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.PreprocessorIf)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorRegion1()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #[|Region|]
        #End Region
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Region)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PreprocessorRegion2()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        #Region
        [|#End|] Region
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Region)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [RaiseEvent]()
            Dim text = <a><![CDATA[
Class Program
    Public Event e as EventHandler
    Sub fooo()
        RaiseEve[||]nt e(nothing, nothing)
    End Sub
End Class]]></a>

            Test(text.Value, "vb.RaiseEvent")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [ReDim]()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim arr(10, 10) as Integer
        ReDim [|Preserve|] array(10, 30)
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Redim)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IsOperator()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Dim a, b as Object
        DIm c = a [|Is|] b
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Is")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [RemoveHandler]()
            Dim text = <a><![CDATA[
Class Program
    Public Event e As EventHandler
    Public Sub EHandler(sender As Object, e As EventArgs)

    End Sub
    Sub fooo()
        Re[||]moveHandler e, AddressOf EHandler
    End Sub
End Class]]></a>

            Test(text.Value, "vb.RemoveHandler")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Resume]()
            Dim text = <a><![CDATA[
Class Program
    Sub fooo()
        Resume [|Next|]
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Resume")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Return]()
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        [|Return|] 3
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Return")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Stop]()
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        St[||]op
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Stop")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [SyncLock]()
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        DIm lock = new Object()
        Syn[||]cLock lock
        End SyncLock
    End Sub
End Class]]></a>

            Test(text.Value, "vb.SyncLock")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub [Throw]()
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        [|Throw|] New System.Exception()
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Throw")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub Negate()
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        Dim x = 3
        y = [|-|]x
    End Sub
End Class]]></a>

            Test(text.Value, HelpKeywords.Negate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub UsingStatement()
            Dim text = <a><![CDATA[
Class Program
    Function fooo() as Integer
        Dim x as IDisposable = nothing
        Us[||]ing x
        End Using
    End Sub
End Class]]></a>

            Test(text.Value, "vb.Using")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub YieldStatement()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        [|Yield|] 1
    End Function
End Class]]></a>

            Test(text.Value, "vb.Yield")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub LocalDeclaration()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        [|Dim|] x = 3
    End Function
End Class]]></a>
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub PredefinedType()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        Dim x as [|Integer|]
    End Function
End Class]]></a>

            Test(text.Value, "vb.Integer")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub IdentifierName()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        System.Console.Wri[||]teLine(2)
    End Function
End Class]]></a>

            Test(text.Value, "System.Console.WriteLine")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub DateLiteral()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Foo() as IEnumerable(of Integer)
        Dim x = #5/30/19[||]90#
    End Function
End Class]]></a>

            Test(text.Value, "vb.Date")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestDocComment()
            Test(<a><![CDATA[Imports System
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
        End Sub

        <WorkItem(864194)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAnonymousType()
            Test(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim mm = Sub(ByRef x As String, y As Integer) System.Console.WriteLine(), k[||]k = Sub(y, x) mm(y, x)
    End Sub
End Class]]></a>.Value, "vb.AnonymousType")
        End Sub

        <WorkItem(864189)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAnonymousProperty()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim query = From iii In {1, 2, 3}
                    Select New With {.P[||]1 = iii}
        Dim i = query.First().P1

    End Sub
End Module]]></a>.Value, "vb.AnonymousType")
        End Sub

        <WorkItem(863684)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestByVal()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(ByV[||]al args As String())

    End Sub
End Module]]></a>.Value, "vb.ByVal")
        End Sub

        <WorkItem(864207)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestOf()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main([||]Of T)(args As String())

    End Sub
End Module]]></a>.Value, "vb.Of")
        End Sub

        <WorkItem(863680)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestCompoundAssign()
            Test(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim i = 0
        i [||]+= 1
        i -= 2
        i *= 3
        i /= 4
    End Sub
End Class
]]></a>.Value, "vb.+=")
        End Sub

        <WorkItem(863661)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestGeneric()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As System.Collections.Generic[||].IEnumerable(Of Integer)

    End Sub
End Module]]></a>.Value, "System.Collections.Generic.IEnumerable`1")
        End Sub

        <WorkItem(863652)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestSub()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        
    End S[||]ub
End Module]]></a>.Value, "vb.Sub")
        End Sub

        <WorkItem(863340)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAsNew()
            Test(<a><![CDATA[Imports System.Text
Public Class Test
    Sub Subroutine()
        Dim sb A[||]s New StringBuilder
    End Sub
End Class
]]></a>.Value, "vb.As")


        End Sub

        <WorkItem(863305)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAwait()
            Test(<a><![CDATA[Public Class Test
    Async Sub AsyncSub()
        Dim x2 = Async Function() As Task(Of Integer)
                     Return A[||]wait AsyncFuncNG(10)
                 End Function
    End Sub
End Class
]]></a>.Value, "vb.Await")
        End Sub

        <WorkItem(864243)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestProperty()
            Test(<a><![CDATA[Class Program
    Prope[||]rty prop As Integer
End Class]]></a>.Value, "vb.AutoImplementedProperty")
        End Sub

        <WorkItem(864226)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestPredefinedTypeMember()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x = Integer.MaxVa[||]lue
    End Sub
End Module]]></a>.Value, "System.Int32.MaxValue")
        End Sub

        <WorkItem(864237)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestModuleModifier()
            Test(<a><![CDATA[Publi[||]c Module M
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
        End Sub

        <WorkItem(864237)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestDelegateModifiers()
            Test(<a><![CDATA[Public Module M
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
        End Sub

        <WorkItem(863273)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAssignment()
            Test(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim x =[||] Int32.Parse("1")
    End Sub
End Class
]]></a>.Value, "vb.=")
        End Sub

        <WorkItem(863228)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestRem()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
        ' COmm[||]ent!
    End Sub
End Module]]></a>.Value, "vb.Rem")
        End Sub

        <WorkItem(863228)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestTodo()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
        ' TODO: COmm[||]ent!
    End Sub
End Module]]></a>.Value, HelpKeywords.TaskListUserComments)
        End Sub

        <WorkItem(863220)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestMethodInvocation()
            Test(<a><![CDATA[Public Class Test
    Sub Subroutine()
    End Sub
    Sub AnotherSub()
        Subroutine()[||]
    End Sub
End Class
]]></a>.Value, "vb.Call")
        End Sub

        <WorkItem(864202)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestImportsXmlns()
            Test(<a><![CDATA[Imports <xmln[||]s:ns="foo">]]></a>.Value, "vb.ImportsXmlns")
        End Sub

        <WorkItem(862420)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestParameter()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(a[||]rgs As String())
        
    End Sub
End Module]]></a>.Value, "System.String()")
        End Sub

        <WorkItem(862396)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestNoToken()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
[||]
    End Sub
End Module]]></a>.Value, "")
        End Sub

        <WorkItem(863293)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestMemberAccess()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Int32.[||]Parse("1")
    End Sub
End Module]]></a>.Value, "System.Int32.Parse")
        End Sub

        <WorkItem(864661)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestCtype2()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

        Dim local5 = If(CTy[||]pe(3, Object), Nothing)
    End Sub
End Module]]></a>.Value, "vb.CType")
        End Sub

        <WorkItem(864661)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestNothing()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

        Dim local5 = If(CType(3, Object), Noth[||]ing)
    End Sub
End Module]]></a>.Value, "vb.Nothing")
        End Sub

        <WorkItem(864658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestNullable()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim Value1a As Integer?[||] = 10
    End Sub
End Module]]></a>.Value, "vb.Nullable")
        End Sub

        <WorkItem(864209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestRegionTrivia()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

#Region "mor[||]e"
#End Region

    End Sub
End Module]]></a>.Value, "vb.String")
        End Sub

        <WorkItem(865034)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestTypeCharacter()
            Test(<a><![CDATA[Public Module M
    Sub M1()
        Dim u = 1[||]UI
        Dim ul = &HBADC0DE
        Dim l = -1L
    End Sub
End Module]]></a>.Value, "vb.UInteger")
        End Sub

        <WorkItem(865061)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestStructure()
            Test(<a><![CDATA[Structure S[||]1
End Structure
]]></a>.Value, "vb.Structure")
        End Sub

        <WorkItem(865047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestXmlLiteralDocument()
            Test(<a><![CDATA[Public Module M
    Sub M1()
        Dim MyXMLLiteral = <?xml versio[||]n="1.0" encoding="utf-8"?>
                           <Details>

                           </Details>

        Dim y = <!-- -->
        Dim z = <e/>

    End Sub
End Module
]]></a>.Value, "vb.XmlLiteralDocument")
        End Sub

        <WorkItem(865047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestXmlEmptyElement()
            Test(<a><![CDATA[Public Module M
    Sub M1()
        Dim MyXMLLiteral = <?xml version="1.0" encoding="utf-8"?>
                           <Details>

                           </Details>

        Dim y = <!-- -->
        Dim z = <e[||]/>

    End Sub
End Module
]]></a>.Value, "vb.XmlLiteralElement")
        End Sub

        <WorkItem(865047)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestXmlLiteralComment()
            Test(<a><![CDATA[Public Module M
    Sub M1()
        Dim MyXMLLiteral = <?xml version="1.0" encoding="utf-8"?>
                           <Details>

                           </Details>

        Dim y = <!--[||] -->
        Dim z = <e/>

    End Sub
End Module
]]></a>.Value, "vb.XmlLiteralComment")
        End Sub

        <WorkItem(865088)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestWhile()
            Test(<a><![CDATA[Class C
    Sub M()
        Dim icount = 0
        Wh[||]ile icount <= 100
            icount += 1
        End While

    End Sub
End Class]]></a>.Value, "vb.While")
        End Sub

        <WorkItem(865326)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestImplements()
            Test(<a><![CDATA[Interface I1
Sub M()
End Interface
Class C
Implements I1
Public Sub M() Imple[||]ments I1.M
End Sub
End Class
]]></a>.Value, "vb.ImplementsClause")
        End Sub

        <WorkItem(865306)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAddressOf()
            Test(<a><![CDATA[Delegate Sub mydele()
Class C
Sub M1()
End Sub
Sub M()
Dim d1 As New mydele(Addre[||]ssOf M1)
Dim addr As mydele = AddressOf M1
End Sub
End Class
]]></a>.Value, "vb.AddressOf")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestShared()
            Test(<a><![CDATA[[|Shared|]]]></a>.Value, "vb.Shared")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestWidening()
            Test(<a><![CDATA[[|Widening|]]]></a>.Value, "vb.Widening")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestCType()
            Test(<a><![CDATA[[|CType|]]]></a>.Value, "vb.CType")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestNarrowing()
            Test(<a><![CDATA[[|Narrowing|]]]></a>.Value, "vb.Narrowing")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestOperator()
            Test(<a><![CDATA[[|Operator|]]]></a>.Value, "vb.Operator")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAddHandler()
            Test(<a><![CDATA[[|AddHandler|]]]></a>.Value, "vb.AddHandler")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAnsi()
            Test(<a><![CDATA[Declare [|Ansi|]]]></a>.Value, "vb.Ansi")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAuto()
            Test(<a><![CDATA[Declare [|Auto|]]]></a>.Value, "vb.Auto")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestUnicode()
            Test(<a><![CDATA[Declare [|Unicode|]]]></a>.Value, "vb.Unicode")
        End Sub

        <WorkItem(898157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestHandles()
            Test(<a><![CDATA[[|Handles|]]]></a>.Value, "vb.Handles")
        End Sub

        <WorkItem(867738)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestImplementsIDisposable()
            Test(<a><![CDATA[Imports System
Class C
    Implements IDis[||]posable
    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Class
]]></a>.Value, "vb.IDisposable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestInherits()
            Test(<a><![CDATA[Imports System
Class C
    Inherits Exc[||]eption

End Class
]]></a>.Value, "System.Exception")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestNot()
            Test(<a><![CDATA[Class C
    Sub M()
        Dim b = False
        b = N[||]ot b
    End Sub
End Class]]></a>.Value, "vb.Not")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestArrayIndex()
            Test(<a><![CDATA[Class C
    Sub M()
        Dim a(4) As Integer
        a[||](0) = 1
    End Sub
End Class]]></a>.Value, "vb.Integer")
        End Sub


        <WorkItem(866074)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestGroupJoin()
            Test(<a><![CDATA[Module LINQ
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
        End Sub

        <WorkItem(866074)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestGroupJoinIn()
            Test(<a><![CDATA[Module LINQ
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
        End Sub

        <WorkItem(866074)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestLinqEquals()
            Test(<a><![CDATA[Module LINQ
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
        End Sub

        <WorkItem(866074)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestSelect()
            Test(<a><![CDATA[Module LINQ
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
        End Sub

        <WorkItem(866074)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestCountLinq()
            Test(<a><![CDATA[Module LINQ
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
        End Sub

        <WorkItem(867747)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestOperatorOverload()
            Test(<a><![CDATA[Class C
    Public Shared Operator IsTr[||]ue(ByVal a As C) As Boolean
        Return False
    End Operator
End Class]]></a>.Value, "vb.IsTrue")
        End Sub

        <WorkItem(866058)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAnonymousLocal()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim produc[||]tList = {New With {.category = "Condiments", .name = "Ketchup"}, New With {.category = "Seafood", .name = "Code"}}
    End Sub
End Module]]></a>.Value, "vb.AnonymousType")
        End Sub

        <WorkItem(866046)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestNoEscaping()
            Test(<a><![CDATA[Imports System
Class C
    Sub M()
        Dim x = "hello"
        Dim t = x.Get[||]Type
    End Sub
End Class]]></a>.Value, "System.Object.GetType")
        End Sub

        <WorkItem(4150, "https://github.com/dotnet/roslyn/issues/4150")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestPropertyFromMemberAccess()
            Test(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        args.Le[||]ngth
    End Sub
End Module]]></a>.Value, "System.Array.Length")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestParameterFromReference()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
        a[||]rgs
    End Sub
End Module]]></a>.Value, "System.String()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestLocalFromReference()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x As Integer
        x[||]
    End Sub
End Module]]></a>.Value, "System.Int32")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestAliasFromReference()
            Test(<a><![CDATA[Imports s = System.Linq.Enumerable

Module Program
    Sub Main(args As String())
        Dim x As s[||]
    End Sub
End Module]]></a>.Value, "System.Linq.Enumerable")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)>
        Public Sub TestRangeVariable()
            Test(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim z = From x In args Select x[||]
    End Sub
End Module]]></a>.Value, "vb.String")
        End Sub
    End Class
End Namespace

