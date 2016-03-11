' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Partial Public Class RenameEngineTests

        Public Class VisualBasicConflicts
            Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

            Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
                _outputHelper = outputHelper
            End Sub

            <WpfFact(Skip:="798375, 799977")>
            <WorkItem(798375, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798375")>
            <WorkItem(773543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773543")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithRollBacksInsideLambdas_2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System

Class C
    Class D
        Public x As Integer = 1
    End Class
    Dim a As Action(Of Integer) = Sub([|$$x|] As Integer)
                                      Dim {|Conflict:y|} = New D()
                                      Console.{|Conflict:WriteLine|}({|Conflict:x|})
                                  End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WpfFact(Skip:="798375")>
            <WorkItem(798375, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798375")>
            <WorkItem(773534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773534")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithRollBacksInsideLambdas()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Structure y
    Public x As Integer
End Structure

Class C
    Class D
        Public x As Integer = 1
        Dim w As Action(Of y) = Sub([|$$x|] As y)
                                    Dim {|Conflict:y|} = New D()
                                    Console.WriteLine(y.x)
                                    Console.WriteLine({|Conflict:x|}.{|Conflict:x|})
                                End Sub
    End Class
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(857937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/857937")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub HandleInvocationExpressions()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module Program
    Sub [|$$Main|](args As String())
        Dim x As New Dictionary(Of Integer, Dictionary(Of Integer, Integer))
        Console.WriteLine(x(1)(3))
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="x")
                End Using
            End Sub

            <Fact>
            <WorkItem(773435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773435")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithInvocationOnDelegateInstance()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Delegate Sub Foo(x As Integer)
    Public Sub FooMeth(x As Integer)
    End Sub
    Public Sub void()
        Dim {|Conflict:x|} As Foo = New Foo(AddressOf FooMeth)
        Dim [|$$z|] As Integer = 1
        Dim y As Integer = {|Conflict:z|}
        x({|Conflict:z|})
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="x")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WorkItem(782020, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782020")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub BreakingRenameWithSameClassInOneNamespace()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports A = N.{|Conflict:X|}

Namespace N
    Class {|Conflict:X|}
    End Class
End Namespace

Namespace N
    Class {|Conflict:$$Y|}
    End Class
End Namespace
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="X")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub OverloadResolutionConflictResolve_1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System
Imports System.Runtime.CompilerServices

Module C
    &lt;Extension()>
    Public Sub Ex(x As String)
    End Sub

    Sub Outer(x As Action(Of String), y As Object)
        Console.WriteLine(1)
    End Sub

    Sub Outer(x As Action(Of Integer), y As Integer)
        Console.WriteLine(2)
    End Sub


    Sub Inner(x As Action(Of String), y As String)
    End Sub
    Sub Inner(x As Action(Of String), y As Integer)
    End Sub
    Sub Inner(x As Action(Of Integer), y As Integer)
    End Sub

    Sub Main()
        {|conflict1:Outer|}(Sub(y) {|conflict2:Inner|}(Sub(x) x.Ex(), y), 0)
        Outer(Sub(y As Integer)
                  Inner(CType(Sub(x)
                                  Console.WriteLine(x)
                                  x.Ex()
                              End Sub, Action(Of String)), y)
              End Sub, 0)
    End Sub
End Module

Module E
    ' Rename Ex To Foo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")

                    result.AssertLabeledSpansAre("conflict2", "Outer(Sub(y As String) Inner(Sub(x) x.Ex(), y), 0)", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("conflict1", "Outer(Sub(y As String) Inner(Sub(x) x.Ex(), y), 0)", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub OverloadResolutionConflictResolve_2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System
Imports System.Runtime.CompilerServices

Module C
    &lt;Extension()>
    Public Sub Ex(x As String)
    End Sub

    Sub Outer(x As Action(Of String), y As Object)
        Console.WriteLine(1)
    End Sub

    Sub Outer(x As Action(Of Integer), y As Integer)
        Console.WriteLine(2)
    End Sub


    Sub Inner(x As Action(Of String), y As String)
    End Sub
    Sub Inner(x As Action(Of String), y As Integer)
    End Sub
    Sub Inner(x As Action(Of Integer), y As Integer)
    End Sub

    Sub Main()
        {|conflict2:Outer|}(Sub(y)
                  {|conflict1:Inner|}(Sub(x)
                                  Console.WriteLine(x)
                                  x.Ex()
                              End Sub, y)
              End Sub, 0)
    End Sub
End Module

Module E
    ' Rename Ex To Foo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")
                    Dim outputResult = <Code>Outer(Sub(y As String)</Code>.Value + vbCrLf +
<Code>                  Inner(Sub(x)</Code>.Value + vbCrLf +
<Code>                                  Console.WriteLine(x)</Code>.Value + vbCrLf +
<Code>                                  x.Ex()</Code>.Value + vbCrLf +
<Code>                              End Sub, y)</Code>.Value + vbCrLf +
<Code>              End Sub, 0)</Code>.Value

                    result.AssertLabeledSpansAre("conflict2", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("conflict1", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub OverloadResolutionConflictResolve_3()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System
Imports System.Runtime.CompilerServices

Module C
    &lt;Extension()>
    Public Sub Ex(x As String)
    End Sub

    Sub Outer(x As Action(Of String), y As Object)
        Console.WriteLine(1)
    End Sub

    Sub Outer(x As Action(Of Integer), y As Integer)
        Console.WriteLine(2)
    End Sub


    Sub Inner(x As Action(Of String), y As String)
    End Sub
    Sub Inner(x As Action(Of String), y As Integer)
    End Sub
    Sub Inner(x As Action(Of Integer), y As Integer)
    End Sub

    Sub Main()
        {|conflict1:Outer|}(Sub(y)
                  {|conflict2:Inner|}(Sub(x)
                            Console.WriteLine(x)
                            Dim z = 5
                            z.{|conflict0:Ex|}()
                            x.Ex()
                        End Sub, y)
              End Sub, 0)
    End Sub
End Module

Module E
    ' Rename Ex To Foo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")
                    Dim outputResult = <Code>Outer(Sub(y As String)</Code>.Value + vbCrLf +
<Code>                  Inner(Sub(x)</Code>.Value + vbCrLf +
<Code>                            Console.WriteLine(x)</Code>.Value + vbCrLf +
<Code>                            Dim z = 5</Code>.Value + vbCrLf +
<Code>                            z.foo()</Code>.Value + vbCrLf +
<Code>                            x.Ex()</Code>.Value + vbCrLf +
<Code>                        End Sub, y)</Code>.Value + vbCrLf +
<Code>              End Sub, 0)</Code>.Value

                    result.AssertLabeledSpansAre("conflict0", outputResult, type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("conflict2", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("conflict1", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub OverloadResolutionConflictResolve_4()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System
Imports System.Runtime.CompilerServices

Module C
    &lt;Extension()>
    Public Sub Ex(x As String)
    End Sub

    Sub Outer(x As Action(Of String), y As Object)
        Console.WriteLine(1)
    End Sub

    Sub Outer(x As Action(Of Integer), y As Integer)
        Console.WriteLine(2)
    End Sub


    Sub Inner(x As Action(Of String), y As String)
    End Sub
    Sub Inner(x As Action(Of String), y As Integer)
    End Sub
    Sub Inner(x As Action(Of Integer), y As Integer)
    End Sub

    Sub Main()
        {|conflict1:Outer|}(Sub(y)
                  {|conflict2:Inner|}(Sub(x)
                            Console.WriteLine(x)
                            Dim z = 5
                            z.{|conflict0:blah|}()
                            x.Ex()
                        End Sub, y)
              End Sub, 0)
    End Sub
End Module

Module E
    ' Rename blah To Ex
    &lt;Extension()>
    Public Sub [|$$blah|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Ex")
                    Dim outputResult = <Code>Outer(Sub(y)</Code>.Value + vbCrLf +
<Code>                  Inner(Sub(x As String)</Code>.Value + vbCrLf +
<Code>                            Console.WriteLine(x)</Code>.Value + vbCrLf +
<Code>                            Dim z = 5</Code>.Value + vbCrLf +
<Code>                            z.Ex()</Code>.Value + vbCrLf +
<Code>                            x.Ex()</Code>.Value + vbCrLf +
<Code>                        End Sub, y)</Code>.Value + vbCrLf +
<Code>              End Sub, 0)</Code>.Value

                    result.AssertLabeledSpansAre("conflict0", outputResult, type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("conflict2", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("conflict1", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameStatementWithResolvingAndUnresolvingConflictInSameStatement_VB()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module Program
    Dim z As Object
    Sub Main(args As String())
        Dim sx = Function([|$$x|] As Integer)
                     {|resolve:z|} = Nothing
                     If (True) Then
                         Dim z As Boolean = {|conflict1:foo|}({|conflict2:x|})
                     End If
                     Return True
                 End Function
    End Sub

    Public Function foo(bar As Integer) As Boolean
        Return True
    End Function

    Public Function foo(bar As Object) As Boolean
        Return bar
    End Function
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="z")

                    result.AssertLabeledSpansAre("conflict2", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("conflict1", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("resolve", "Program.z = Nothing", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

#Region "Type Argument Expand/Reduce for Generic Method Calls - 639136"

            <WorkItem(729401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729401")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub IntroduceWhitespaceTriviaToInvocationIfCallKeywordIsIntroduced()
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.vb">
Module M
    Public Sub [|$$Foo|](Of T)(ByVal x As T) ' Rename Foo to Bar
    End Sub
End Module
 
Class C
    Public Sub Bar(ByVal x As String)
    End Sub
    Class M
        Public Shared Bar As Action(Of String) = Sub(ByVal x As String)
                                                 End Sub
    End Class
    Public Sub Test()
        {|stmt1:Foo|}("1")
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "Call Global.M.Bar(""1"")", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem(728646, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728646")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ExpandInvocationInStaticMemberAccess()
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class CC
    Public Shared Sub [|$$Foo|](Of T)(x As T)

    End Sub
    Public Shared Sub Bar(x As Integer)

    End Sub
    Public Sub Baz()

    End Sub
End Class

Class D
    Public Sub Baz()
        CC.{|stmt1:Foo|}(1)
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "CC.Bar(Of Integer)(1)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem(725934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/725934"), WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact()>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_Me()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Public Sub TestMethod()
        Dim x = 1
        Dim y = {|stmt1:F|}(x)
    End Sub

    Public Function F(Of T)(x As T) As Integer
        Return 1
    End Function

    Public Function [|$$B|](x As Integer) As Integer
        Return 1
    End Function
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="F")


                    result.AssertLabeledSpansAre("stmt1", "Dim y = F(Of Integer)(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Shared Sub F(Of T)(x As Func(Of Integer, T))

    End Sub

    Shared Sub [|$$B|](x As Func(Of Integer, Integer))

    End Sub

    Shared Sub main()
        {|stmt1:F|}(Function(a) a)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="F")


                    result.AssertLabeledSpansAre("stmt1", "F(Of Integer)(Function(a) a)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_Nested()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub [|$$Foo|](Of T)(ByVal x As T)

    End Sub
    Public Shared Sub Bar(ByVal x As Integer)

    End Sub
    Class D
        Sub Bar(Of T)(ByVal x As T)

        End Sub
        Sub Bar(ByVal x As Integer)

        End Sub
        Sub Test()
            {|stmt1:Foo|}(1)
        End Sub
    End Class
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "C.Bar(Of Integer)(1)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_ReferenceType()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module M
    Public Sub Foo(Of T)(ByVal x As T)

    End Sub
    Public Sub [|$$Bar|](ByVal x As String)

    End Sub
    Public Sub Test()
        Dim x = "1"
        {|stmt1:Foo|}(x)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of String)(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136"), WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103"), WorkItem(755801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755801")>
            <WpfFact(Skip:="755801"), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_Cref()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Public Sub Foo(Of T)(ByVal x As T)
    End Sub
    ''' <summary>
    ''' <see cref="{|stmt1:Foo|}"/>
    ''' </summary>
    ''' <param name="x"></param>
    Public Sub [|$$Bar|](ByVal x As Integer)

    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of T)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_DifferentScope1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module M
    Public Sub [|$$Foo|](Of T)(ByVal x As T) ' Rename Foo to Bar
    End Sub
    Public Sub Bar(ByVal x As Integer)

    End Sub
End Module

Class C
    Public Sub Bar(ByVal x As Integer)
    End Sub
    Class M
        Public Shared Bar As Action(Of Integer) = Sub(ByVal x As Integer)
                                                  End Sub
    End Class
    Public Sub Test()
        {|stmt1:Foo|}(1)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "Call Global.M.Bar(Of Integer)(1)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_ConstructedTypeArgumentGenericContainer()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C(Of S)
    Public Shared Sub Foo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](ByVal x As C(Of Integer))

    End Sub
    Public Sub Test()
        Dim x As C(Of Integer) = New C(Of Integer)()
        {|stmt1:Foo|}(x)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of C(Of Integer))(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_ConstructedTypeArgumentNonGenericContainer()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Foo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](ByVal x As D(Of Integer))

    End Sub
    Public Sub Test()
        Dim x As D(Of Integer) = New D(Of Integer)()
        {|stmt1:Foo|}(x)
    End Sub
End Class
Class D(Of S)
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of D(Of Integer))(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_ObjectType()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Foo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](ByVal x As Object)

    End Sub
    Public Sub Test()
        Dim x = DirectCast(1, Object)
        {|stmt1:Foo|}(x)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of Object)(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_SameTypeParameter()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Foo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](Of T)(ByVal x As T())

    End Sub
    Public Sub Test()
        Dim x As Integer() = New Integer() {1, 2, 3}
        {|stmt1:Foo|}(x)
    End Sub
End Class

                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of Integer())(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_MultiDArrayTypeParameter()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Foo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](Of T)(ByVal x As T(,))

    End Sub
    Public Sub Test()
        Dim x As Integer(,) = New Integer(,) {{1, 2}, {2, 3}, {3, 4}}
        {|stmt1:Foo|}(x)
    End Sub
End Class

                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of Integer(,))(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_UsedAsArgument()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Function Foo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As Integer) As Integer
        Return 1
    End Function
    Public Sub Method(ByVal x As Integer)

    End Sub
    Public Sub Test()
        Method({|stmt1:Foo|}(1))
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Method(Foo(Of Integer)(1))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_UsedInConstructorInitialization()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Sub New(ByVal x As Integer)

    End Sub
    Public Function Foo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As Integer) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x As New C({|stmt1:Foo|}(1))
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Dim x As New C(Foo(Of Integer)(1))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_CalledOnObject()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Function Foo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As Integer) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x As New C()
        x.{|stmt1:Foo|}(1)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "x.Foo(Of Integer)(1)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_UsedInGenericDelegate()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Delegate Function FooDel(Of T)(ByVal x As T) As Integer
    Public Function Foo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As String) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x = New FooDel(Of String)(AddressOf {|stmt1:Foo|})
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Dim x = New FooDel(Of String)(AddressOf Foo(Of String))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_UsedInNonGenericDelegate()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Delegate Function FooDel(ByVal x As String) As Integer
    Public Function Foo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As String) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x = New FooDel(AddressOf {|stmt1:Foo|})
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Dim x = New FooDel(AddressOf Foo(Of String))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_MultipleTypeParameters()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Foo(Of T, S)(ByVal x As T, ByVal y As S)

    End Sub
    Public Shared Sub [|$$Bar|](Of T, S)(ByVal x As T(), ByVal y As S)

    End Sub
    Public Sub Test()
        Dim x = New Integer() {1, 2}
        {|stmt1:Foo|}(x, New C())
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "Foo(Of Integer(), C)(x, New C())", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(639136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <WorkItem(730781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730781")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ConflictResolutionWithTypeInference_ConflictInDerived()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Sub Foo(Of T)(ByRef x As T)

    End Sub
    Public Sub Foo(ByRef x As String)

    End Sub
End Class

Class D
    Inherits C

    Public Sub [|$$Bar|](ByRef x As Integer)

    End Sub
    Public Sub Test()
        Dim x As String
        {|stmt1:Foo|}(x)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("stmt1", "MyBase.Foo(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub
#End Region

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithInstanceField()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class FooClass
    Private foo As Integer

    Sub Blah([|$$bar|] As Integer)
        {|stmt2:foo|} = {|stmt1:bar|}
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("stmt1", "Me.foo = foo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "Me.foo = foo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithInstanceFieldRenamingToKeyword()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class FooClass
    Private [if] As Integer

    Sub Blah({|Escape:$$bar|} As Integer)
        {|stmt2:[if]|} = {|stmt1:bar|}
    End Sub
End Class
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="if")

                    result.AssertLabeledSpecialSpansAre("Escape", "[if]", RelatedLocationType.NoConflict)

                    ' we don't unescape [if] in Me.[if] because the user gave it to us escaped.
                    result.AssertLabeledSpecialSpansAre("stmt1", "Me.[if] = [if]", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpecialSpansAre("stmt2", "Me.[if] = [if]", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithInstanceFieldRenamingToKeyword2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class FooClass
    Private {|escape:$$bar|} As Integer

    Sub Blah([if] As Integer)
        {|stmt1:bar|} = [if]
    End Sub
End Class
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="if")

                    result.AssertLabeledSpecialSpansAre("escape", "[if]", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt1", "Me.if = [if]", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithSharedField()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class FooClass
    Shared foo As Integer

    Sub Blah([|$$bar|] As Integer)
        {|stmt2:foo|} = {|stmt1:bar|}
    End Sub
End Class
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("stmt1", "FooClass.foo = foo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "FooClass.foo = foo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ParameterConflictingWithFieldInModule()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module FooModule
    Private foo As Integer

    Sub Blah([|$$bar|] As Integer)
        {|stmt2:foo|} = {|stmt1:bar|}
    End Sub
End Module
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("stmt1", "FooModule.foo = foo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "FooModule.foo = foo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MinimalQualificationOfBaseType1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class X
                                    Protected Class [|$$A|]
                                    End Class
                                End Class
                                
                                Class Y
                                    Inherits X
                                
                                    Protected Class C
                                        Inherits {|Resolve:A|}
                                    End Class
                                
                                    Class B
                                    End Class
                                End Class
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="B")


                    result.AssertLabeledSpansAre("Resolve", "X.B", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MinimalQualificationOfBaseType2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                 Class X
                                     Protected Class A
                                     End Class
                                 End Class
                                 
                                 Class Y
                                     Inherits X
                                 
                                     Protected Class C
                                         Inherits {|Resolve:A|}
                                     End Class
                                 
                                     Class [|$$B|]
                                     End Class
                                 End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="A")


                    result.AssertLabeledSpansAre("Resolve", "X.A", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub PreserveTypeCharactersForKeywordsAsIdentifiers()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Option Strict On

Class C
    Sub New
        Dim x$ = Me.{|stmt1:$$Foo|}.ToLower
    End Sub

    Function {|TypeSuffix:Foo$|}
        Return "ABC"
    End Function
End Class

                               </Document>
                        </Project>
                    </Workspace>, renameTo:="Class")

                    result.AssertLabeledSpansAre("stmt1", "Class", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpecialSpansAre("TypeSuffix", "Class$", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(529695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529695")>
            <WorkItem(543016, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543016")>
            <WpfFact(Skip:="529695"), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameDoesNotBreakQuery()
                Using result = RenameEngineResult.Create(_outputHelper,
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim col1 As New Col
        Dim query = From i In col1 Select i
    End Sub
End Module
 
Public Class Col
    Function {|escaped:$$[Select]|}(ByVal sel As Func(Of Integer, Integer)) As IEnumerable(Of Integer)
        Return Nothing
    End Function
End Class
                                </Document>
                            </Project>
                        </Workspace>, renameTo:="FooSelect")

                    result.AssertLabeledSpansAre("escaped", "[FooSelect]", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WpfFact(Skip:="566460")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(566460, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566460")>
            <WorkItem(542349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542349")>
            Public Sub ProperlyEscapeNewKeywordWithTypeCharacters()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Option Strict On

Class C
    Sub New
        Dim x$ = Me.{|stmt1:$$Foo$|}.ToLower
    End Sub

    Function {|Unescaped:Foo$|}
        Return "ABC"
    End Function
End Class
                               </Document>
                        </Project>
                    </Workspace>, renameTo:="New")

                    result.AssertLabeledSpansAre("Unescaped", "New$", type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt1", "Dim x$ = Me.[New].ToLower", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub AvoidDoubleEscapeAttempt()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Module [|$$Program|]
                                    Sub Main()

                                    End Sub
                                End Module
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="[true]")


                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ReplaceAliasWithNestedGenericType()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                   
                                   Imports C = A(Of Integer).B
                                   
                                   Class A(Of T)
                                       Class B
                                       End Class
                                   End Class
                                   
                                   Module M
                                       Sub Main
                                           Dim x As {|stmt1:C|}
                                       End Sub
                                   
                                       Class [|D$$|]
                                       End Class
                                   End Module

                               </Document>
                        </Project>
                    </Workspace>, renameTo:="C")


                    result.AssertLabeledSpansAre("stmt1", "Dim x As A(Of Integer).B", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(540440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540440")>
            Public Sub RenamingFunctionWithFunctionVariableFromFunction()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module Program
    Function [|$$X|]() As Integer
        {|stmt1:X|} = 1
    End Function
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")


                    result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(540440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540440")>
            Public Sub RenamingFunctionWithFunctionVariableFromFunctionVariable()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module Program
    Function [|X|]() As Integer
        {|stmt1:$$X|} = 1
    End Function
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")


                    result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WpfFact(Skip:="566542")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542999")>
            <WorkItem(566542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566542")>
            Public Sub ResolveConflictingTypeIncludedThroughModule1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Inherits N.{|Replacement:A|}
End Class
Namespace N
    Module X
        Class A
        End Class
    End Module
    Module Y
        Class [|$$B|]
        End Class
    End Module
End Namespace
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="A")


                    result.AssertLabeledSpansAre("Replacement", "N.X.A", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WpfFact(Skip:="566542")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(543068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543068")>
            <WorkItem(566542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566542")>
            Public Sub ResolveConflictingTypeIncludedThroughModule2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Inherits {|Replacement:N.{|Resolved:A|}|}
End Class
Namespace N
    Module X
        Class [|$$A|]
        End Class
    End Module
    Module Y
        Class B
        End Class
    End Module
End Namespace
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="B")


                    result.AssertLabeledSpansAre("Replacement", "N.X.B")
                    result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(543068, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543068")>
            Public Sub ResolveConflictingTypeImportedFromMultipleTypes()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports X
Imports Y

Module Program
    Sub Main
        {|stmt1:Foo|} = 1
    End Sub
End Module

Class X
    Public Shared [|$$Foo|]
End Class

Class Y
    Public Shared Bar
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "X.Bar = 1", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542936")>
            Public Sub ConflictWithImplicitlyDeclaredLocal()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Option Explicit Off
Module Program
    Function [|$$Foo|]
        {|Conflict:Bar|} = 1
    End Function
End Module
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(542886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542886")>
            Public Sub RenameForRangeVariableUsedInLambda()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Module Program
    Sub Main(args As String())
        For {|stmt1:$$i|} = 1 To 20
            Dim q As Action = Sub()
                                  Console.WriteLine({|stmt1:i|})
                              End Sub
        Next
    End Sub
End Module
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="j")

                    result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <WorkItem(543021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543021")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ShouldNotCascadeToExplicitlyImplementedInterfaceMethodOfDifferentName()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Public Interface MyInterface
    Sub Bar()
End Interface

Public Structure MyStructure
    Implements MyInterface 

    Private Sub [|$$I_Bar|]() Implements MyInterface.Bar
    End Sub
End Structure
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Baz")


                End Using
            End Sub

            <Fact>
            <WorkItem(543021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543021")>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub ShouldNotCascadeToImplementingMethodOfDifferentName()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Public Interface MyInterface
    Sub [|$$Bar|]()
End Interface

Public Structure MyStructure
    Implements MyInterface 

    Private Sub I_Bar() Implements MyInterface.[|Bar|]
    End Sub
End Structure
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Baz")


                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAttributeSuffix()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|Special:Something|}()>
Public class foo
End class

Public Class [|$$SomethingAttribute|]
	Inherits Attribute
End Class]]></Document>
                        </Project>
                    </Workspace>, renameTo:="SpecialAttribute")


                    result.AssertLabeledSpansAre("Special", "Special", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAttributeFromUsage()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|Special:Something|}()>
Public class foo
End class

Public Class {|Special:$$SomethingAttribute|}
	Inherits Attribute
End Class]]></Document>
                        </Project>
                    </Workspace>, renameTo:="Special")

                    result.AssertLabeledSpansAre("Special", "Special", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(543488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543488")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameFunctionCallAfterElse()
                ' This is a simple scenario but it has a somewhat strange tree in VB. The
                ' BeginTerminator of the ElseBlockSyntax is missing, and just so happens to land at
                ' the same location as the NewMethod invocation that follows the Else.
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Module Program
    Sub Main(ByRef args As String())
        If (True)
        Else {|stmt1:NewMethod|}() :
        End If
    End Sub
    Private Sub [|$$NewMethod|]()
    End Sub
End Module
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="NewMethod1")


                    result.AssertLabeledSpansAre("stmt1", "NewMethod1", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(11004, "DevDiv_Projects/Roslyn")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameImplicitlyDeclaredLocal()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Option Explicit Off
 
Module Program
    Sub Main(args As String())
        {|stmt1:$$foo|} = 23
        {|stmt2:foo|} = 42
    End Sub
End Module
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="barbaz")


                    result.AssertLabeledSpansAre("stmt1", "barbaz", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "barbaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(11004, "DevDiv_Projects/Roslyn")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameFieldToConflictWithImplicitlyDeclaredLocal()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Option Explicit Off
 
Module Program
    Dim [|$$bar|] As Object
 
    Sub Main(args As String())
        {|stmt1_2:foo|} = {|stmt1:bar|}
        {|stmt2:foo|} = 42
    End Sub
End Module

                           </Document>
                        </Project>
                    </Workspace>, renameTo:="foo")


                    result.AssertLabeledSpansAre("stmt1", "foo", type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt1_2", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("stmt2", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WorkItem(543420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543420")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameParameterOfEvent()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Class Test
    Public Event Percent(ByVal [|$$p|] As Single)
    Public Shared Sub Main()
    End Sub
End Class
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="barbaz")


                End Using
            End Sub

            <WorkItem(543587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543587")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameLocalInMethodMissingParameterList()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports System

Module Program
    Sub Main
        Dim {|stmt1:$$a|} As Integer
    End Sub
End Module
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="barbaz")

                    result.AssertLabeledSpansAre("stmt1", "barbaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(542649, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542649")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub QualifyTypeWithGlobalWhenConflicting()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Class A
End Class

Class B
    Dim x As {|Resolve:A|}

    Class [|$$C|]
    End Class
End Class
                           </Document>
                        </Project>
                    </Workspace>, renameTo:="A")


                    result.AssertLabeledSpansAre("Resolve", "Global.A", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(542322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub QualifyFieldInReDimStatement()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Module Preserve
    Sub Main
        Dim Bar
        ReDim {|stmt1:Foo|}(0)
    End Sub
 
    Property [|$$Foo|]
End Module
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("stmt1", "ReDim [Preserve].Bar(0)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem(566542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566542")>
            <WorkItem(545604, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545604")>
            <WpfFact(Skip:="566542"), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub QualifyTypeNameInImports()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports {|Resolve:X|}

Module M
    Class X
    End Class
End Module

Module N
    Class [|$$Y|] ' Rename Y to X
    End Class
End Module
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="X")


                    result.AssertLabeledSpansAre("Resolve", "M.X", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameNewOverload()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports System
Module Program
    Sub Main()
        {|ResolvedNonReference:Foo|}(Sub(x) x.{|Resolve:Old|}())
    End Sub
    Sub Foo(x As Action(Of I))
    End Sub
    Sub Foo(x As Action(Of C))
    End Sub
End Module

Interface I
    Sub {|Escape:$$Old|}()
End Interface

Class C
    Sub [New]()
    End Sub
End Class
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="New")

                    result.AssertLabeledSpecialSpansAre("Escape", "[New]", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Resolve", "Foo(Sub(x) x.New())", RelatedLocationType.ResolvedReferenceConflict)
                    result.AssertLabeledSpansAre("ResolvedNonReference", "Foo(Sub(x) x.New())", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAttributeRequiringReducedNameToResolveConflict()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Public Class [|$$YAttribute|]
    Inherits System.Attribute
End Class

Public Class ZAttributeAttribute
    Inherits System.Attribute
End Class

<{|resolve:YAttribute|}>
Class Class1
End Class

Class Class2
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="ZAttribute")

                    result.AssertLabeledSpecialSpansAre("resolve", "Z", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameEvent()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports System
Namespace N
    Public Interface I
        Event [|$$X|] As EventHandler ' Rename X to Y
    End Interface
End Namespace
                        </Document>
                        </Project>
                        <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                            <ProjectReference>VBAssembly</ProjectReference>
                            <Document FilePath="Test.cs">
using System;
using N;
class C : I
{
    event EventHandler I.[|X|]
    {
        add { }
        remove { }
    }
}
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="Y")


                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameInterfaceImplementation()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports System
Interface I
    Sub Foo(Optional x As Integer = 0)
End Interface
Class C
    Implements I
    Shared Sub Main()
        DirectCast(New C(), I).Foo()
    End Sub
    Private Sub [|$$I_Foo|](Optional x As Integer = 0) Implements I.Foo
        Console.WriteLine("test")
    End Sub
End Class
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameAttributeConflictWithNamespace()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System
Namespace X
    Class [|$$A|] ' Rename A to B
        Inherits Attribute
    End Class

    Namespace N.BAttribute
        <{|Resolve:A|}>
        Delegate Sub F()
    End Namespace
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="B")


                    result.AssertLabeledSpansAre("Resolve", "X.B", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameREMToUnicodeREM()
                Dim text = ChrW(82) & ChrW(69) & ChrW(77)
                Dim compareText = "[" & text & "]"
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Module {|Resolve:$$[REM]|}
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:=text)

                    result.AssertLabeledSpecialSpansAre("Resolve", compareText, RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameImports()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports [|$$S|] = System.Collections
Imports System
Namespace X
    <A>
    Class A
        Inherits {|Resolve1:Attribute|}
    End Class
End Namespace

Module M
   Dim a As {|Resolve2:S|}.ArrayList
End Module
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Attribute")


                    result.AssertLabeledSpansAre("Resolve1", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Resolve2", "Attribute", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(578105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578105")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug578105_VBRenamingPartialMethodDifferentCasing()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Class Foo    
    Partial Private Sub [|Foo|]()
    End Sub

    Private Sub [|$$foo|]()
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Baz")


                End Using
            End Sub

            <WorkItem(588142, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/588142")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug588142_SimplifyAttributeUsageCanAlwaysEscapeInVB()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|escaped:A|}>
Class [|$$AAttribute|] ' Rename A to RemAttribute
    Inherits Attribute
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="RemAttribute")


                    result.AssertLabeledSpansAre("escaped", "[Rem]", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem(588038, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/588038")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug588142_RenameAttributeToAttribute()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|unreduced:Foo|}>
Class [|$$FooAttribute|] ' Rename Foo to Attribute
    Inherits {|resolved:Attribute|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Attribute")


                    result.AssertLabeledSpansAre("unreduced", "Attribute", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolved", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(576573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576573")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug576573_ConflictAttributeWithNamespace()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

Namespace X
    Class B
        Inherits Attribute
    End Class

    Namespace N.[|$$Y|] ' Rename Y to BAttribute
        <{|resolved:B|}>
        Delegate Sub F()
    End Namespace
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="BAttribute")


                    result.AssertLabeledSpansAre("resolved", "X.B", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(603368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603368")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug603368_ConflictAttributeWithNamespaceCaseInsensitive()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

Namespace X
    Class B
        Inherits Attribute
    End Class

    Namespace N.[|$$Y|] ' Rename Y to BAttribute
        <{|resolved:B|}>
        Delegate Sub F()
    End Namespace
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="BATTRIBUTE")


                    result.AssertLabeledSpansAre("resolved", "X.B", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(603367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603367")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug603367_ConflictAttributeWithNamespaceCaseInsensitive2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|resolved:Foo|}>
Module M
    Class FooAttribute
        Inherits Attribute
    End Class
End Module
 
Class [|$$X|] ' Rename X to FOOATTRIBUTE
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="FOOATTRIBUTE")


                    result.AssertLabeledSpansAre("resolved", "M.Foo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(603276, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603276")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug603276_ConflictAttributeWithNamespaceCaseInsensitive3()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<[|Foo|]>
Class [|$$Foo|] ' Rename Foo to ATTRIBUTE
    Inherits {|resolved:Attribute|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="ATTRIBUTE")


                    result.AssertLabeledSpansAre("resolved", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(529712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529712")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug529712_ConflictNamespaceWithModuleName_1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Module Program
    Sub Main()
        N.{|resolved:Foo|}()
    End Sub
End Module
 
Namespace N
    Namespace [|$$Y|] ' Rename Y to Foo
    End Namespace
    Module X
        Sub Foo()
        End Sub
    End Module
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("resolved", "N.X.Foo()", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(529837, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529837")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug529837_ResolveConflictByOmittingModuleName()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
                                Namespace X
                                    Public Module Y
                                        Public Class C
                                        End Class
                                    End Module
                                End Namespace
                             </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document>
                                Namespace X
                                    Namespace Y
                                        Class [|$$D|]
                                            Inherits {|resolved:C|}
                                        End Class
                                    End Namespace
                                End Namespace
                             </Document>
                        </Project>
                    </Workspace>, renameTo:="C")


                    result.AssertLabeledSpansAre("resolved", "X.C", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(529989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529989")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub Bug529989_RenameCSharpIdentifierToInvalidVBIdentifier()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="Project1" CommonReferences="true">
                            <Document>
                                public class {|invalid:$$ProgramCS|}
                                {
                                }
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                            <ProjectReference>Project1</ProjectReference>
                            <Document>
                                Module ProgramVB
                                    Sub Main(args As String())
                                        Dim d As {|invalid:ProgramCS|}
                                    End Sub
                                End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="B\u0061r")

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("invalid", "B\u0061r", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameModuleBetweenAssembly()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <ProjectReference>Project2</ProjectReference>
                            <Document>
Imports System
Module Program
    Sub Main(args As String())
        Dim {|Stmt1:$$Bar|} = Sub(x) Console.Write(x)
        Call {|Resolve:Foo|}()
        {|Stmt2:Bar|}(1)
    End Sub
End Module                   
                         </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                            <Document>
Public Module M
    Sub Foo()
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")

                    result.AssertLabeledSpansAre("Stmt1", "Foo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Stmt2", "Foo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Resolve", "Call M.Foo()", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameModuleClassConflict()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Namespace N
    Module M
        Class C
            Shared Sub Foo()

            End Sub
        End Class
    End Module
    Class [|$$D|]
        Shared Sub Foo()

        End Sub
    End Class
    Module Program
        Sub Main()
            {|Resolve:C|}.{|Resolve:Foo|}()    
            {|Stmt1:D|}.Foo()
        End Sub
    End Module 
End Namespace
                       
                             </Document>
                        </Project>
                    </Workspace>, renameTo:="C")


                    result.AssertLabeledSpansAre("Resolve", "M.C.Foo()", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Stmt1", "C", RelatedLocationType.NoConflict)

                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameModuleNamespaceNested()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Namespace N
    Namespace M
        Module K
            Sub Foo()

            End Sub
        End Module
        Module L
            Sub [|$$Bar|]()

            End Sub
        End Module
    End Namespace
End Namespace
Module Program
    Sub Main(args As String())
        N.M.{|Resolve1:Foo|}()
        N.M.{|Resolve2:Bar|}()
    End Sub
End Module                       
                             </Document>
                        </Project>
                    </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("Resolve1", "N.M.K.Foo()", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Resolve2", "N.M.L.Foo()", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameModuleConflictWithInterface()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Interface M
    Sub foo(ByVal x As Integer)
End Interface
Namespace N
    Module [|$$K|]
        Sub foo(ByVal x As Integer)

        End Sub
    End Module
    Class C
        Implements {|Resolve:M|}
        Public Sub foo(x As Integer) Implements {|Resolve:M|}.foo
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace                             </Document>
                        </Project>
                    </Workspace>, renameTo:="M")


                    result.AssertLabeledSpansAre("Resolve", "Global.M", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(628700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/628700")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameModuleConflictWithLocal()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Namespace N
    Class D
        Public x() As Integer = {0, 1}
    End Class
    Module M
        Public x() As Integer = {0, 1}
    End Module
    Module S
        Dim M As New D()
        Dim [|$$y|] As Integer
        Dim p = From x In M.x Select x
        Dim q = From x In {|Resolve:x|} Select x
    End Module
End Namespace
                             </Document>
                        </Project>
                    </Workspace>, renameTo:="x")


                    result.AssertLabeledSpansAre("Resolve", "N.M.x", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(633180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633180")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub VB_DetectOverLoadResolutionChangesInEnclosingInvocations()
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.cs">
Imports System
Imports System.Runtime.CompilerServices

Module C
    &lt;Extension()>
    Public Sub Ex(x As String)
    End Sub

    Sub Outer(x As Action(Of String), y As Object)
        Console.WriteLine(1)
    End Sub

    Sub Outer(x As Action(Of Integer), y As Integer)
        Console.WriteLine(2)
    End Sub


    Sub Inner(x As Action(Of String), y As String)
    End Sub
    Sub Inner(x As Action(Of String), y As Integer)
    End Sub
    Sub Inner(x As Action(Of Integer), y As Integer)
    End Sub

    Sub Main()
        {|resolved:Outer|}(Sub(y) {|resolved:Inner|}(Sub(x) x.Ex(), y), 0)
    End Sub
End Module

Module E
    ' Rename Ex To Foo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                    result.AssertLabeledSpansAre("resolved", "Outer(Sub(y As String) Inner(Sub(x) x.Ex(), y), 0)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(673562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562"), WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameNamespaceConflictsAndResolves()
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Namespace NN
    Class C
        ''' &lt;see cref="{|resolve1:NN|}.C"/&gt;  
        Public x As {|resolve2:NN|}.C
    End Class

    Namespace [|$$KK|]
        Class C

        End Class
    End Namespace
End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="NN")


                    result.AssertLabeledSpansAre("resolve1", "Global.NN.C", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", "Global.NN", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(673667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameUnnecessaryExpansion()
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.cs">
Namespace N
    Class C
        Public x As {|resolve:N|}.C
    End Class

    Class [|$$D|]
        Class C
            Public y As [|D|]
        End Class
    End Class
End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="N")


                    result.AssertLabeledSpansAre("resolve", "Global.N", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(645152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645152")>
            <Fact(), Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub AdjustTriviaForExtensionMethodRewrite()
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.cs">
Imports System.Runtime.CompilerServices
 
Class C
Sub Bar(tag As Integer)
        Me.{|resolve:Foo|}(1).{|resolve:Foo|}(2)
    End Sub
End Class
 
Module E
    &lt;Extension&gt;
    Public Function [|$$Foo|](x As C, tag As Integer) As C
        Return x
    End Function
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Bar")


                    result.AssertLabeledSpansAre("resolve", "E.Bar(E.Bar(Me,1),2)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem(569103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameCrefWithConflict()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports F = N
Namespace N
    Interface I
        Sub Foo()
    End Interface
End Namespace

Class C
	Private Class E
        Implements {|Resolve:F|}.I
        ''' <summary>
        ''' This is a function <see cref="{|Resolve:F|}.I.Foo"/>
        ''' </summary>
        Public Sub Foo() Implements {|Resolve:F|}.I.Foo
        End Sub
    End Class
	Private Class [|$$K|]
	End Class
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="F")


                    result.AssertLabeledSpansAre("Resolve", "N", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(768910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768910")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub RenameInCrefPreservesWhitespaceTrivia()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.vb">
                                <![CDATA[
Public Class A
    Public Class B
        Public Class C

        End Class
        ''' <summary>
        ''' <see cref=" {|Resolve:D|}"/>  
        ''' ''' </summary>
        Shared Sub [|$$foo|]()    ' Rename foo to D 
        End Sub
    End Class
    Public Class D
    End Class
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="D")


                    result.AssertLabeledSpansAre("Resolve", "A.D", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(1016652, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016652")>
            Public Sub VB_ConflictBetweenTypeNamesInTypeConstraintSyntax()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports System.Collections.Generic

Public Interface {|unresolved1:$$INamespaceSymbol|}
End Interface

Public Interface {|DeclConflict:ISymbol|}
End Interface

Public Interface IReferenceFinder
End Interface

Friend MustInherit Partial Class AbstractReferenceFinder(Of TSymbol As {|unresolved2:INamespaceSymbol|})
	Implements IReferenceFinder

End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, renameTo:="ISymbol")

                    result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("unresolved1", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("unresolved2", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(905, "https://github.com/dotnet/roslyn/issues/905")>
            Public Sub RenamingCompilerGeneratedPropertyBackingField_InvokeFromProperty()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Class C1
    Public ReadOnly Property [|X$$|] As String

    Sub M()
        {|backingfield:_X|} = "test"
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Y")

                    result.AssertLabeledSpecialSpansAre("backingfield", "_Y", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(905, "https://github.com/dotnet/roslyn/issues/905")>
            Public Sub RenamingCompilerGeneratedPropertyBackingField_IntroduceConflict()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Class C1
    Public ReadOnly Property [|X$$|] As String

    Sub M()
        {|Conflict:_X|} = "test"
    End Sub

    Dim _Y As String
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WpfFact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(905, "https://github.com/dotnet/roslyn/issues/905")>
            Public Sub RenamingCompilerGeneratedPropertyBackingField_InvokableFromBackingFieldReference()
                Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Class C1
    Public ReadOnly Property [|X|] As String

    Sub M()
        {|backingfield:_X$$|} = "test"
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    AssertTokenRenamable(workspace)
                End Using
            End Sub

            <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MemberQualificationInNameOfUsesTypeName_StaticReferencingInstance()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Class C
    Shared Sub F([|$$z|] As Integer)
        Dim x = NameOf({|ref:zoo|})
    End Sub

    Dim zoo As Integer
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="zoo")

                    result.AssertLabeledSpansAre("ref", "Dim x = NameOf(C.zoo)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MemberQualificationInNameOfUsesTypeName_InstanceReferencingStatic()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Class C
    Sub F([|$$z|] As Integer)
        Dim x = NameOf({|ref:zoo|})
    End Sub

    Shared zoo As Integer
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="zoo")

                    result.AssertLabeledSpansAre("ref", "Dim x = NameOf(C.zoo)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(1193, "https://github.com/dotnet/roslyn/issues/1193")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub MemberQualificationInNameOfUsesTypeName_InstanceReferencingInstance()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Class C
    Sub F([|$$z|] As Integer)
        Dim x = NameOf({|ref:zoo|})
    End Sub

    Dim zoo As Integer
End Class
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="zoo")

                    result.AssertLabeledSpansAre("ref", "Dim x = NameOf(C.zoo)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub TestConflictBetweenClassAndInterface1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class {|conflict:C|}
End Class
Interface [|$$I|]
End Interface
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="C")

                    result.AssertLabeledSpansAre("conflict", "C", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub TestConflictBetweenClassAndInterface2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class [|$$C|]
End Class
Interface {|conflict:I|}
End Interface
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="I")

                    result.AssertLabeledSpansAre("conflict", "I", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub TestConflictBetweenClassAndNamespace1()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class {|conflict:$$C|}
End Class
Namespace N
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="N")

                    result.AssertLabeledSpansAre("conflict", "N", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub TestConflictBetweenClassAndNamespace2()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class {|conflict:C|}
End Class
Namespace [|$$N|]
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="C")

                    result.AssertLabeledSpansAre("conflict", "C", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <WorkItem(1027506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub TestNoConflictBetweenTwoNamespaces()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Namespace [|$$N1|]
End Namespace
Namespace N2
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="N2")
                End Using
            End Sub

            <WorkItem(1195, "https://github.com/dotnet/roslyn/issues/1195")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub NameOfReferenceNoConflict()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class C
    Sub [|T$$|](x As Integer)
    End Sub

    Sub Test()
        Dim x = NameOf(Test)
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Test")
                End Using
            End Sub

            <WorkItem(1195, "https://github.com/dotnet/roslyn/issues/1195")>
            <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub NameOfReferenceWithConflict()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class C
    Sub Test()
        Dim [|T$$|] As Integer
        Dim x = NameOf({|conflict:Test|})
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="Test")

                    result.AssertLabeledSpansAre("conflict", "Test", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WorkItem(1031, "https://github.com/dotnet/roslyn/issues/1031")>
            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub InvalidNamesDoNotCauseCrash_IntroduceQualifiedName()
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class {|conflict:C$$|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="C.D")

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("conflict", "C.D", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WorkItem(1031, "https://github.com/dotnet/roslyn/issues/1031")>
            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            Public Sub InvalidNamesDoNotCauseCrash_AccidentallyPasteLotsOfCode()
                Dim renameTo = "
Class C
    Sub M()
        System.Console.WriteLine(""Hello, Test!"")
    End Sub
End Class"
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class {|conflict:C$$|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, renameTo)

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("conflict", renameTo, RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(7440, "https://github.com/dotnet/roslyn/issues/7440")>
            Public Sub RenameTypeParameterInPartialClass()
                Using result = RenameEngineResult.Create(_outputHelper,
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document><![CDATA[
Partial Class C(Of [|$$T|])
End Class

Partial Class C(Of [|T|])
End Class
]]>
                                </Document>
                            </Project>
                        </Workspace>, renameTo:="T2")
                End Using
            End Sub

            <Fact>
            <Trait(Traits.Feature, Traits.Features.Rename)>
            <WorkItem(7440, "https://github.com/dotnet/roslyn/issues/7440")>
            Public Sub RenameMethodToConflictWithTypeParameter()
                Using result = RenameEngineResult.Create(_outputHelper,
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document><![CDATA[
Partial Class C(Of {|Conflict:T|})
    Sub [|$$M|]()
    End Sub
End Class

Partial Class C(Of {|Conflict:T|})
End Class
]]>
                                </Document>
                            </Project>
                        </Workspace>, renameTo:="T")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub
        End Class
    End Class
End Namespace
