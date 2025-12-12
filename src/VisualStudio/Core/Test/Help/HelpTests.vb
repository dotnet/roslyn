' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Help
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Help
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.F1Help)>
    Public Class HelpTests
        Public Shared Async Function TestAsync(markup As String, expected As String) As Tasks.Task
            Using workspace = TestWorkspace.CreateVisualBasic(markup)
                Dim caret = workspace.Documents.First().CursorPosition
                Dim service = New VisualBasicHelpContextService()
                Assert.Equal(expected, Await service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None))
            End Using
        End Function

        <Fact>
        Public Async Function TestFriend() As Task
            Dim text = <a>
Fri[||]end Class G
End Class</a>

            Await TestAsync(text.Value, "vb.Friend")
        End Function

        <Fact>
        Public Async Function TestProtected() As Task
            Dim text = <a>
Public Class G
    Protec[||]ted Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.Protected")
        End Function

        <Fact>
        Public Async Function TestProtectedFriend1() As Task
            Dim text = <a>
Public Class G
    Protec[||]ted Friend Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.ProtectedFriend")
        End Function

        <Fact>
        Public Async Function TestProtectedFriend2() As Task
            Dim text = <a>
Public Class G
    Friend Protec[||]ted Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.ProtectedFriend")
        End Function

        <Fact>
        Public Async Function TestPrivateProtected1() As Task
            Dim text = <a>
Public Class G
    Private Protec[||]ted Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.PrivateProtected")
        End Function

        <Fact>
        Public Async Function TestPrivateProtected2() As Task
            Dim text = <a>
Public Class G
    Priv[||]ate Protected Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.PrivateProtected")
        End Function

        <Fact>
        Public Async Function TestPrivateProtected3() As Task
            Dim text = <a>
Public Class G
    Protected Priv[||]ate Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.PrivateProtected")
        End Function

        <Fact>
        Public Async Function TestPrivateProtected4() As Task
            Dim text = <a>
Public Class G
    Protec[||]ted Private Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.PrivateProtected")
        End Function

        <Fact>
        Public Async Function TestModifierSoup() As Task
            Dim text = <a>
Public Class G
    Protec[||]ted Async Shared Private Sub M()
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.PrivateProtected")
        End Function

        <Fact>
        Public Async Function TestModifierSoupField() As Task
            Dim text = <a>
Public Class G
    Private Shadows Shared Prot[||]ected foo as Boolean
End Class</a>

            Await TestAsync(text.Value, "vb.PrivateProtected")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestArrayInitializer() As Task
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() = new Integer() {1,[||] 2, 3}
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.Array")
        End Function

        <Fact>
        Public Async Function TestArrayInitializer2() As Task
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() = new[||] Integer() {1, 2, 3}
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.Array")
        End Function

        <Fact>
        Public Async Function TestAssignment1() As Task
            Dim text = <a>
Class G
    Public Sub G()
        Dim x as integer() =[||] new {1, 2, 3}
    End Sub
End Class</a>

            Await TestAsync(text.Value, "vb.=")
        End Function

        <Fact>
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

        <Fact>
        Public Async Function TestModuleAttribute() As Task
            Dim text = <a><![CDATA[
Imports System.Reflection
<Assembly: AssemblyTitleAttribute("Production assembly 4"), Mod[||]ule: CLSCompliant(True)>
Module M

End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.ModuleAttribute)
        End Function

        <Fact>
        Public Async Function TestAssemblyAttribute() As Task
            Dim text = <a><![CDATA[
Imports System.Reflection
<Ass[||]embly: AssemblyTitleAttribute("Production assembly 4"), Module: CLSCompliant(True)>
Module M

End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.AssemblyAttribute)
        End Function

        <Fact>
        Public Async Function TestBinaryOperator() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        DIm x = 2 +[||] 3
    End Sub
ENd Class]]></a>

            Await TestAsync(text.Value, "vb.+")
        End Function

        <Fact>
        Public Async Function TestCallStatement() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        C[||]all G()
    End Sub
ENd Class]]></a>

            Await TestAsync(text.Value, "vb.Call")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestConstructor() As Task
            Dim text = <a><![CDATA[
Class G
    Sub Ne[||]w()
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Constructor)
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestEndBlockKind() As Task
            Dim text = <a><![CDATA[
Class G
En[||]d Class]]></a>

            Await TestAsync(text.Value, "vb.Class")
        End Function

        <Fact>
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

        <Fact>
        Public Async Function TestEndSub() As Task
            Dim text = <a><![CDATA[
Class G
    Sub goo()
        End[||]
    ENd Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.End")
        End Function

        <Fact>
        Public Async Function TestEnumMember() As Task
            Dim text = <a><![CDATA[
Enum G
    A[||]
End Enum]]></a>

            Await TestAsync(text.Value, "vb.Enum")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestError() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Er[||]ror 1
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Error")
        End Function

        <Fact>
        Public Async Function TestEvent() As Task
            Dim text = <a><![CDATA[
Class G
    Ev[||]ent e As EventHandler
End Class]]></a>

            Await TestAsync(text.Value, "vb.Event")
        End Function

        <Fact>
        Public Async Function TestExit1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub Goo()
        While True
            Exit [|While|]
        End While
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Exit")
        End Function

        <Fact>
        Public Async Function TestExit2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub Goo()
        While True
            Exit While
        End While
        Exit [|Sub|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Exit")
        End Function

        <Fact>
        Public Async Function TestField1() As Task
            Dim text = <a><![CDATA[
Class G
    Protec[||]ted goo as Integer
End Class]]></a>

            Await TestAsync(text.Value, "vb.Protected")
        End Function

        <Fact>
        Public Async Function TestField2() As Task
            Dim text = <a><![CDATA[
Class G
    Protected ReadOn[||]ly goo as Integer
End Class]]></a>

            Await TestAsync(text.Value, "vb.ReadOnly")
        End Function

        <Fact>
        Public Async Function TestField3() As Task
            Dim text = <a><![CDATA[
Class G
    [|Dim|] goo as Integer
End Class]]></a>

            Await TestAsync(text.Value, "vb.Dim")
        End Function

        <Fact>
        Public Async Function TestForEach() As Task
            Dim text = <a><![CDATA[
Class G
    For each x [|in|] {0}
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.In")
        End Function

        <Fact>
        Public Async Function TestForEach2() As Task
            Dim text = <a><![CDATA[
Class G
    For [|each|] x in {0}
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.Each")
        End Function

        <Fact>
        Public Async Function TestForEach3() As Task
            Dim text = <a><![CDATA[
Class G
    [|For|] each x in {0}
    NExt
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.ForEach)
        End Function

        <Fact>
        Public Async Function TestFor() As Task
            Dim text = <a><![CDATA[
Class G
    For x = 1 to 3 [|Step|] 2
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.Step")
        End Function

        <Fact>
        Public Async Function TestFor2() As Task
            Dim text = <a><![CDATA[
Class G
    For x = 1 [|to|] 3 Step 2
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.To")
        End Function

        <Fact>
        Public Async Function TestFor3() As Task
            Dim text = <a><![CDATA[
Class G
    [|Fo|]r x = 1 to 3 Step 2
    NExt
End Class]]></a>

            Await TestAsync(text.Value, "vb.For")
        End Function

        <Fact>
        Public Async Function TestFrom() As Task
            Dim text = <a><![CDATA[
Class G
    Dim z = F[||]rom x in {1 2 3} select x
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.QueryFrom)
        End Function

        <Fact>
        Public Async Function TestTypeParameter1() As Task
            Dim text = <a><![CDATA[
Interface I(Of [|Out|] R)
    Function Do() as R
End Interface]]></a>

            Await TestAsync(text.Value, HelpKeywords.VarianceOut)
        End Function

        <Fact>
        Public Async Function TestGetType() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = GetT[|y|]pe(G)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.GetType")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestIfOperator() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim x = [|If|](true, 0, 1)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.IfOperator)
        End Function

        <Fact>
        Public Async Function TestImplements1() As Task
            Dim text = <a><![CDATA[
Interface IGoo 
End Interface
Interface IBar
End Interface
Class G
    [|Implements|] IGoo, Ibar
End Class]]></a>

            Await TestAsync(text.Value, "vb.Implements")
        End Function

        <Fact>
        Public Async Function TestImplements2() As Task
            Dim text = <a><![CDATA[
Interface IGoo 
End Interface
Interface IBar
End Interface
Class G
    Implements IGoo[|,|] IBar
End Class]]></a>

            Await TestAsync(text.Value, "vb.Implements")
        End Function

        <Fact>
        Public Async Function TestAnonymousType1() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = [|New|] With {Key .Goo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact>
        Public Async Function TestAnonymousType2() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New [|With|] {Key .Goo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact>
        Public Async Function TestAnonymousType3() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {[|Key|] .Goo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousKey)
        End Function

        <Fact>
        Public Async Function TestAnonymousType4() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {Key [|.Goo|] = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact>
        Public Async Function TestJoinOn() As Task
            Dim text = <a><![CDATA[
Class G
    Sub G()
        Dim f1 = New With {Key [|.Goo|] = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.AnonymousType)
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestPartialMethod() As Task
            Dim text = <a><![CDATA[
Class G
    [|Partial|] Sub Goo()
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PartialMethod)
        End Function

        <Fact>
        Public Async Function TestMainMethod() As Task
            Dim text = <a><![CDATA[
Module Goo
    Sub m[||]ain()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.Main)
        End Function

        <Fact>
        Public Async Function TestMeToken() As Task
            Dim text = <a><![CDATA[
Module Goo
    Sub main()
        [|Me|].main()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.Me")
        End Function

        <Fact>
        Public Async Function TestConstructRatherThanName() As Task
            Dim text = <a><![CDATA[
Module [|Goo|]
    Sub main()
        main()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.Module")
        End Function

        <Fact>
        Public Async Function TestMyBase() As Task
            Dim text = <a><![CDATA[
Class Goo
    Sub main()
        My[|Base|].GetType()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.MyBase")
        End Function

        <Fact>
        Public Async Function TestMyClass() As Task
            Dim text = <a><![CDATA[
Class Goo
    Sub main()
        My[|Base|].GetType()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, "vb.MyBase")
        End Function

        <Fact>
        Public Async Function TestNewConstraint() As Task
            Dim text = <a><![CDATA[
Interface IBar
End Interface
Class Goo(Of T As {IBar, [|New|]})
    Sub main()
        MyBase.GetType()
    End Sub
End Module]]></a>

            Await TestAsync(text.Value, HelpKeywords.NewConstraint)
        End Function

        <Fact>
        Public Async Function TestObjectInitializer() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Property goo As Integer
    Sub gooo()
        Dim p = New Program [|With|] {.goo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.ObjectInitializer)
        End Function

        <Fact>
        Public Async Function TestNothingToken() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Property goo As Integer
    Sub gooo()
        Dim p = New Program [|With|] {.goo = 3}
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.ObjectInitializer)
        End Function

        <Fact>
        Public Async Function TestNullable1() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Dim [|p?|] as boolean
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Nullable)
        End Function

        <Fact>
        Public Async Function TestOnError() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        On Error Resume [|Next|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OnError)
        End Function

        <Fact>
        Public Async Function TestOptionCompare() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Option Compare [|Binary|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionCompare)
        End Function

        <Fact>
        Public Async Function TestOptionExplicit() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Option Explicit [|Off|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionExplicit)
        End Function

        <Fact>
        Public Async Function TestOptionInfer() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Option Infer [|Off|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionInfer)
        End Function

        <Fact>
        Public Async Function TestOptionStrict() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Option Strict [|Off|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.OptionStrict)
        End Function

        <Fact>
        Public Async Function TestOption() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        [|Option|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Option")
        End Function

        <Fact>
        Public Async Function TestPredefinedCast() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Dim x = [|CInt|](1)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.CInt")
        End Function

        <Fact>
        Public Async Function TestDirectCast() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Dim x = [|DirectCast|](1, Object)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.DirectCast")
        End Function

        <Fact>
        Public Async Function TestTryCast() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Dim x = [|TryCast|](1, Object)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.TryCast")
        End Function

        <Fact>
        Public Async Function TestPreprocessorConst() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #const x [|=|] 3
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorConst)
        End Function

        <Fact>
        Public Async Function TestPreprocessorConditional1() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #[|If|] true Then
        #ElseIF Flase Then
        #Else
        #End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact>
        Public Async Function TestPreprocessorConditional2() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #If true Then
        #[|ElseIf|] Flase Then
        #Else
        #End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact>
        Public Async Function TestPreprocessorConditional3() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #If true Then
        #ElseIf Flase Then
        #[|Else|]
        #End If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact>
        Public Async Function TestPreprocessorConditional4() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #If true Then
        #ElseIf Flase Then
        #Else
        #[|End|] If
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.PreprocessorIf)
        End Function

        <Fact>
        Public Async Function TestPreprocessorRegion1() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #[|Region|]
        #End Region
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Region)
        End Function

        <Fact>
        Public Async Function TestPreprocessorRegion2() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        #Region
        [|#End|] Region
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Region)
        End Function

        <Fact>
        Public Async Function TestRaiseEvent() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Event e as EventHandler
    Sub gooo()
        RaiseEve[||]nt e(nothing, nothing)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.RaiseEvent")
        End Function

        <Fact>
        Public Async Function TestReDim() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Dim arr(10, 10) as Integer
        ReDim [|Preserve|] array(10, 30)
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Redim)
        End Function

        <Fact>
        Public Async Function TestIsOperator() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Dim a, b as Object
        DIm c = a [|Is|] b
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Is")
        End Function

        <Fact>
        Public Async Function TestRemoveHandler() As Task
            Dim text = <a><![CDATA[
Class Program
    Public Event e As EventHandler
    Public Sub EHandler(sender As Object, e As EventArgs)

    End Sub
    Sub gooo()
        Re[||]moveHandler e, AddressOf EHandler
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.RemoveHandler")
        End Function

        <Fact>
        Public Async Function TestResume() As Task
            Dim text = <a><![CDATA[
Class Program
    Sub gooo()
        Resume [|Next|]
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Resume")
        End Function

        <Fact>
        Public Async Function TestReturn() As Task
            Dim text = <a><![CDATA[
Class Program
    Function gooo() as Integer
        [|Return|] 3
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Return")
        End Function

        <Fact>
        Public Async Function TestStop() As Task
            Dim text = <a><![CDATA[
Class Program
    Function gooo() as Integer
        St[||]op
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Stop")
        End Function

        <Fact>
        Public Async Function TestSyncLock() As Task
            Dim text = <a><![CDATA[
Class Program
    Function gooo() as Integer
        DIm lock = new Object()
        Syn[||]cLock lock
        End SyncLock
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.SyncLock")
        End Function

        <Fact>
        Public Async Function TestThrow() As Task
            Dim text = <a><![CDATA[
Class Program
    Function gooo() as Integer
        [|Throw|] New System.Exception()
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Throw")
        End Function

        <Fact>
        Public Async Function TestNegate() As Task
            Dim text = <a><![CDATA[
Class Program
    Function gooo() as Integer
        Dim x = 3
        y = [|-|]x
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, HelpKeywords.Negate)
        End Function

        <Fact>
        Public Async Function TestUsingStatement() As Task
            Dim text = <a><![CDATA[
Class Program
    Function gooo() as Integer
        Dim x as IDisposable = nothing
        Us[||]ing x
        End Using
    End Sub
End Class]]></a>

            Await TestAsync(text.Value, "vb.Using")
        End Function

        <Fact>
        Public Async Function TestYieldStatement() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Goo() as IEnumerable(of Integer)
        [|Yield|] 1
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "vb.Yield")
        End Function

        <Fact>
        Public Sub TestLocalDeclaration()
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Goo() as IEnumerable(of Integer)
        [|Dim|] x = 3
    End Function
End Class]]></a>
        End Sub

        <Fact>
        Public Async Function TestPredefinedType() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Goo() as IEnumerable(of Integer)
        Dim x as [|Integer|]
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "vb.Integer")
        End Function

        <Fact>
        Public Async Function TestIdentifierName() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Goo() as IEnumerable(of Integer)
        System.Console.Wri[||]teLine(2)
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "System.Console.WriteLine")
        End Function

        <Fact>
        Public Async Function TestDateLiteral() As Task
            Dim text = <a><![CDATA[
Class Program
    Private Iterator Function Goo() as IEnumerable(of Integer)
        Dim x = #5/30/19[||]90#
    End Function
End Class]]></a>

            Await TestAsync(text.Value, "vb.Date")
        End Function

        <Fact>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864194")>
        Public Async Function TestAnonymousType() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim mm = Sub(ByRef x As String, y As Integer) System.Console.WriteLine(), k[||]k = Sub(y, x) mm(y, x)
    End Sub
End Class]]></a>.Value, "vb.AnonymousType")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864189")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863684")>
        Public Async Function TestByVal() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(ByV[||]al args As String())

    End Sub
End Module]]></a>.Value, "vb.ByVal")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864207")>
        Public Async Function TestOf() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main([||]Of T)(args As String())

    End Sub
End Module]]></a>.Value, "vb.Of")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863680")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863661")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863652")>
        Public Async Function TestSub() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        
    End S[||]ub
End Module]]></a>.Value, "vb.Sub")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863340")>
        Public Async Function TestAsNew() As Task
            Await TestAsync(<a><![CDATA[Imports System.Text
Public Class Test
    Sub Subroutine()
        Dim sb A[||]s New StringBuilder
    End Sub
End Class
]]></a>.Value, "vb.As")

        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863305")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864243")>
        Public Async Function TestProperty() As Task
            Await TestAsync(<a><![CDATA[Class Program
    Prope[||]rty prop As Integer
End Class]]></a>.Value, "vb.AutoImplementedProperty")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864226")>
        Public Async Function TestPredefinedTypeMember() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x = Integer.MaxVa[||]lue
    End Sub
End Module]]></a>.Value, "System.Int32.MaxValue")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864237")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864237")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863273")>
        Public Async Function TestAssignment() As Task
            Await TestAsync(<a><![CDATA[Public Class Test
    Sub Subroutine()
        Dim x =[||] Int32.Parse("1")
    End Sub
End Class
]]></a>.Value, "vb.=")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863228")>
        Public Async Function TestRem() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        ' COmm[||]ent!
    End Sub
End Module]]></a>.Value, "vb.Rem")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863228")>
        Public Async Function TestTodo() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        ' TODO: COmm[||]ent!
    End Sub
End Module]]></a>.Value, HelpKeywords.TaskListUserComments)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863220")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864202")>
        Public Async Function TestImportsXmlns() As Task
            Await TestAsync(<a><![CDATA[Imports <xmln[||]s:ns="goo">]]></a>.Value, "vb.ImportsXmlns")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")>
        Public Async Function TestParameter() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(a[||]rgs As String())
        
    End Sub
End Module]]></a>.Value, "System.String()")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862396")>
        Public Async Function TestNoToken() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
[||]
    End Sub
End Module]]></a>.Value, "")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863293")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864661")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864661")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864658")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864209")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865034")>
        Public Async Function TestTypeCharacter() As Task
            Await TestAsync(<a><![CDATA[Public Module M
    Sub M1()
        Dim u = 1[||]UI
        Dim ul = &HBADC0DE
        Dim l = -1L
    End Sub
End Module]]></a>.Value, "vb.UInteger")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865061")>
        Public Async Function TestStructure() As Task
            Await TestAsync(<a><![CDATA[Structure S[||]1
End Structure
]]></a>.Value, "vb.Structure")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865047")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865047")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865047")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865088")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865326")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865306")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestShared() As Task
            Await TestAsync(<a><![CDATA[[|Shared|]]]></a>.Value, "vb.Shared")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestWidening() As Task
            Await TestAsync(<a><![CDATA[[|Widening|]]]></a>.Value, "vb.Widening")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestCType() As Task
            Await TestAsync(<a><![CDATA[[|CType|]]]></a>.Value, "vb.CType")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestNarrowing() As Task
            Await TestAsync(<a><![CDATA[[|Narrowing|]]]></a>.Value, "vb.Narrowing")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestOperator() As Task
            Await TestAsync(<a><![CDATA[[|Operator|]]]></a>.Value, "vb.Operator")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestAddHandler() As Task
            Await TestAsync(<a><![CDATA[[|AddHandler|]]]></a>.Value, "vb.AddHandler")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestAnsi() As Task
            Await TestAsync(<a><![CDATA[Declare [|Ansi|]]]></a>.Value, "vb.Ansi")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestAuto() As Task
            Await TestAsync(<a><![CDATA[Declare [|Auto|]]]></a>.Value, "vb.Auto")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestUnicode() As Task
            Await TestAsync(<a><![CDATA[Declare [|Unicode|]]]></a>.Value, "vb.Unicode")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898157")>
        Public Async Function TestHandles() As Task
            Await TestAsync(<a><![CDATA[[|Handles|]]]></a>.Value, "vb.Handles")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867738")>
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

        <Fact>
        Public Async Function TestInherits() As Task
            Await TestAsync(<a><![CDATA[Imports System
Class C
    Inherits Exc[||]eption

End Class
]]></a>.Value, "System.Exception")
        End Function

        <Fact>
        Public Async Function TestNot() As Task
            Await TestAsync(<a><![CDATA[Class C
    Sub M()
        Dim b = False
        b = N[||]ot b
    End Sub
End Class]]></a>.Value, "vb.Not")
        End Function

        <Fact>
        Public Async Function TestArrayIndex() As Task
            Await TestAsync(<a><![CDATA[Class C
    Sub M()
        Dim a(4) As Integer
        a[||](0) = 1
    End Sub
End Class]]></a>.Value, "vb.Integer")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866074")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866074")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866074")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866074")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866074")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867747")>
        Public Async Function TestOperatorOverload() As Task
            Await TestAsync(<a><![CDATA[Class C
    Public Shared Operator IsTr[||]ue(ByVal a As C) As Boolean
        Return False
    End Operator
End Class]]></a>.Value, "vb.IsTrue")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866058")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866046")>
        Public Async Function TestNoEscaping() As Task
            Await TestAsync(<a><![CDATA[Imports System
Class C
    Sub M()
        Dim x = "hello"
        Dim t = x.Get[||]Type
    End Sub
End Class]]></a>.Value, "System.Object.GetType")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4150")>
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

        <Fact>
        Public Async Function TestParameterFromReference() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        a[||]rgs
    End Sub
End Module]]></a>.Value, "System.String()")
        End Function

        <Fact>
        Public Async Function TestLocalFromReference() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x As Integer
        x[||]
    End Sub
End Module]]></a>.Value, "System.Int32")
        End Function

        <Fact>
        Public Async Function TestAliasFromReference() As Task
            Await TestAsync(<a><![CDATA[Imports s = System.Linq.Enumerable

Module Program
    Sub Main(args As String())
        Dim x As s[||]
    End Sub
End Module]]></a>.Value, "System.Linq.Enumerable")
        End Function

        <Fact>
        Public Async Function TestRangeVariable() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim z = From x In args Select x[||]
    End Sub
End Module]]></a>.Value, "vb.String")
        End Function

        <Fact>
        Public Async Function CaretAfterMemberAccessDot() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x = (2).[||]ToString()
    End Sub
End Module]]></a>.Value, "System.Int32.ToString")
        End Function

        <Fact>
        Public Async Function CaretBeforeMemberAccessDot() As Task
            Await TestAsync(<a><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x = (2)[||].ToString()
    End Sub
End Module]]></a>.Value, "System.Int32.ToString")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68003")>
        Public Async Function TestNameOfExpression() As Task
            Await TestAsync(<a><![CDATA[Module Program
    Sub Main(args As String())
        Dim x = NameOf[||](args)
    End Sub
End Module]]></a>.Value, "vb.NameOf")
        End Function
    End Class
End Namespace

