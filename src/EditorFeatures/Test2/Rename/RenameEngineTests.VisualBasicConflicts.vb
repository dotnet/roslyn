' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Partial Public Class RenameEngineTests
        <UseExportProvider>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Class VisualBasicConflicts
            Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

            Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
                _outputHelper = outputHelper
            End Sub

            <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798375")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773543")>
            <CombinatorialData>
            Public Sub BreakingRenameWithRollBacksInsideLambdas_2(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798375")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773534")>
            <CombinatorialData>
            Public Sub BreakingRenameWithRollBacksInsideLambdas(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/857937")>
            <CombinatorialData>
            Public Sub HandleInvocationExpressions(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="x")
                End Using
            End Sub

            <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773435")>
            <CombinatorialData>
            Public Sub BreakingRenameWithInvocationOnDelegateInstance(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Delegate Sub Goo(x As Integer)
    Public Sub GooMeth(x As Integer)
    End Sub
    Public Sub void()
        Dim {|Conflict:x|} As Goo = New Goo(AddressOf GooMeth)
        Dim [|$$z|] As Integer = 1
        Dim y As Integer = {|Conflict:z|}
        x({|Conflict:z|})
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="x")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782020")>
            Public Sub BreakingRenameWithSameClassInOneNamespace(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="X")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub OverloadResolutionConflictResolve_1(host As RenameTestHost)
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
    ' Rename Ex To Goo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="z")

                    result.AssertLabeledSpansAre("conflict2", "Outer(Sub(y As String) Inner(Sub(x) x.Ex(), y), 0)", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("conflict1", "Outer(Sub(y As String) Inner(Sub(x) x.Ex(), y), 0)", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub OverloadResolutionConflictResolve_2(host As RenameTestHost)
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
    ' Rename Ex To Goo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="z")
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

            <Theory, CombinatorialData>
            Public Sub OverloadResolutionConflictResolve_3(host As RenameTestHost)
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
    ' Rename Ex To Goo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo")
                    Dim outputResult = <Code>Outer(Sub(y As String)</Code>.Value + vbCrLf +
<Code>                  Inner(Sub(x)</Code>.Value + vbCrLf +
<Code>                            Console.WriteLine(x)</Code>.Value + vbCrLf +
<Code>                            Dim z = 5</Code>.Value + vbCrLf +
<Code>                            z.goo()</Code>.Value + vbCrLf +
<Code>                            x.Ex()</Code>.Value + vbCrLf +
<Code>                        End Sub, y)</Code>.Value + vbCrLf +
<Code>              End Sub, 0)</Code>.Value

                    result.AssertLabeledSpansAre("conflict0", outputResult, type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("conflict2", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("conflict1", outputResult, type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub OverloadResolutionConflictResolve_4(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Ex")
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

            <Theory, CombinatorialData>
            Public Sub RenameStatementWithResolvingAndUnresolvingConflictInSameStatement_VB(host As RenameTestHost)
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
                         Dim z As Boolean = {|conflict1:goo|}({|conflict2:x|})
                     End If
                     Return True
                 End Function
    End Sub

    Public Function goo(bar As Integer) As Boolean
        Return True
    End Function

    Public Function goo(bar As Object) As Boolean
        Return bar
    End Function
End Module
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="z")

                    result.AssertLabeledSpansAre("conflict2", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("conflict1", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("resolve", "Program.z = Nothing", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

#Region "Type Argument Expand/Reduce for Generic Method Calls - 639136"

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729401")>
            Public Sub IntroduceWhitespaceTriviaToInvocationIfCallKeywordIsIntroduced(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.vb">
Module M
    Public Sub [|$$Goo|](Of T)(ByVal x As T) ' Rename Goo to Bar
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
        {|stmt1:Goo|}("1")
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("stmt1", "Call Global.M.Bar(""1"")", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728646")>
            Public Sub ExpandInvocationInStaticMemberAccess(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class CC
    Public Shared Sub [|$$Goo|](Of T)(x As T)

    End Sub
    Public Shared Sub Bar(x As Integer)

    End Sub
    Public Sub Baz()

    End Sub
End Class

Class D
    Public Sub Baz()
        CC.{|stmt1:Goo|}(1)
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("stmt1", "CC.Bar(Of Integer)(1)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/725934"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <CombinatorialData>
            Public Sub ConflictResolutionWithTypeInference_Me(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="F")

                    result.AssertLabeledSpansAre("stmt1", "Dim y = F(Of Integer)(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="F")

                    result.AssertLabeledSpansAre("stmt1", "F(Of Integer)(Function(a) a)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_Nested(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub [|$$Goo|](Of T)(ByVal x As T)

    End Sub
    Public Shared Sub Bar(ByVal x As Integer)

    End Sub
    Class D
        Sub Bar(Of T)(ByVal x As T)

        End Sub
        Sub Bar(ByVal x As Integer)

        End Sub
        Sub Test()
            {|stmt1:Goo|}(1)
        End Sub
    End Class
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("stmt1", "C.Bar(Of Integer)(1)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_ReferenceType(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module M
    Public Sub Goo(Of T)(ByVal x As T)

    End Sub
    Public Sub [|$$Bar|](ByVal x As String)

    End Sub
    Public Sub Test()
        Dim x = "1"
        {|stmt1:Goo|}(x)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of String)(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755801")>
            <WpfTheory(Skip:="755801"), CombinatorialData>
            Public Sub ConflictResolutionWithTypeInference_Cref(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Class C
    Public Sub Goo(Of T)(ByVal x As T)
    End Sub
    ''' <summary>
    ''' <see cref="{|stmt1:Goo|}"/>
    ''' </summary>
    ''' <param name="x"></param>
    Public Sub [|$$Bar|](ByVal x As Integer)

    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of T)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_DifferentScope1(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module M
    Public Sub [|$$Goo|](Of T)(ByVal x As T) ' Rename Goo to Bar
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
        {|stmt1:Goo|}(1)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("stmt1", "Call Global.M.Bar(Of Integer)(1)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_ConstructedTypeArgumentGenericContainer(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C(Of S)
    Public Shared Sub Goo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](ByVal x As C(Of Integer))

    End Sub
    Public Sub Test()
        Dim x As C(Of Integer) = New C(Of Integer)()
        {|stmt1:Goo|}(x)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of C(Of Integer))(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_ConstructedTypeArgumentNonGenericContainer(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Goo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](ByVal x As D(Of Integer))

    End Sub
    Public Sub Test()
        Dim x As D(Of Integer) = New D(Of Integer)()
        {|stmt1:Goo|}(x)
    End Sub
End Class
Class D(Of S)
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of D(Of Integer))(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_ObjectType(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Goo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](ByVal x As Object)

    End Sub
    Public Sub Test()
        Dim x = DirectCast(1, Object)
        {|stmt1:Goo|}(x)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of Object)(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_SameTypeParameter(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Goo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](Of T)(ByVal x As T())

    End Sub
    Public Sub Test()
        Dim x As Integer() = New Integer() {1, 2, 3}
        {|stmt1:Goo|}(x)
    End Sub
End Class

                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of Integer())(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_MultiDArrayTypeParameter(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Goo(Of T)(ByVal x As T)

    End Sub
    Public Shared Sub [|$$Bar|](Of T)(ByVal x As T(,))

    End Sub
    Public Sub Test()
        Dim x As Integer(,) = New Integer(,) {{1, 2}, {2, 3}, {3, 4}}
        {|stmt1:Goo|}(x)
    End Sub
End Class

                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of Integer(,))(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_UsedAsArgument(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Function Goo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As Integer) As Integer
        Return 1
    End Function
    Public Sub Method(ByVal x As Integer)

    End Sub
    Public Sub Test()
        Method({|stmt1:Goo|}(1))
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Method(Goo(Of Integer)(1))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_UsedInConstructorInitialization(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Sub New(ByVal x As Integer)

    End Sub
    Public Function Goo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As Integer) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x As New C({|stmt1:Goo|}(1))
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Dim x As New C(Goo(Of Integer)(1))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_CalledOnObject(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Function Goo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As Integer) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x As New C()
        x.{|stmt1:Goo|}(1)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "x.Goo(Of Integer)(1)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_UsedInGenericDelegate(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Delegate Function GooDel(Of T)(ByVal x As T) As Integer
    Public Function Goo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As String) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x = New GooDel(Of String)(AddressOf {|stmt1:Goo|})
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Dim x = New GooDel(Of String)(AddressOf Goo(Of String))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_UsedInNonGenericDelegate(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Delegate Function GooDel(ByVal x As String) As Integer
    Public Function Goo(Of T)(ByVal x As T) As Integer
        Return 1
    End Function
    Public Function [|$$Bar|](ByVal x As String) As Integer
        Return 1
    End Function
    Public Sub Method()
        Dim x = New GooDel(AddressOf {|stmt1:Goo|})
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Dim x = New GooDel(AddressOf Goo(Of String))", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            Public Sub ConflictResolutionWithTypeInference_MultipleTypeParameters(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Shared Sub Goo(Of T, S)(ByVal x As T, ByVal y As S)

    End Sub
    Public Shared Sub [|$$Bar|](Of T, S)(ByVal x As T(), ByVal y As S)

    End Sub
    Public Sub Test()
        Dim x = New Integer() {1, 2}
        {|stmt1:Goo|}(x, New C())
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "Goo(Of Integer(), C)(x, New C())", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/639136")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/730781")>
            Public Sub ConflictResolutionWithTypeInference_ConflictInDerived(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class C
    Public Sub Goo(Of T)(ByRef x As T)

    End Sub
    Public Sub Goo(ByRef x As String)

    End Sub
End Class

Class D
    Inherits C

    Public Sub [|$$Bar|](ByRef x As Integer)

    End Sub
    Public Sub Test()
        Dim x As String
        {|stmt1:Goo|}(x)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("stmt1", "MyBase.Goo(x)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub
#End Region

            <Theory>
            <CombinatorialData>
            Public Sub ParameterConflictingWithInstanceField(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class GooClass
    Private goo As Integer

    Sub Blah([|$$bar|] As Integer)
        {|stmt2:goo|} = {|stmt1:bar|}
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo")

                    result.AssertLabeledSpansAre("stmt1", "Me.goo = goo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "Me.goo = goo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub ParameterConflictingWithInstanceFieldRenamingToKeyword(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class GooClass
    Private [if] As Integer

    Sub Blah({|Escape:$$bar|} As Integer)
        {|stmt2:[if]|} = {|stmt1:bar|}
    End Sub
End Class
                               </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="if")

                    result.AssertLabeledSpecialSpansAre("Escape", "[if]", RelatedLocationType.NoConflict)

                    ' we don't unescape [if] in Me.[if] because the user gave it to us escaped.
                    result.AssertLabeledSpecialSpansAre("stmt1", "Me.[if] = [if]", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpecialSpansAre("stmt2", "Me.[if] = [if]", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub ParameterConflictingWithInstanceFieldRenamingToKeyword2(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class GooClass
    Private {|escape:$$bar|} As Integer

    Sub Blah([if] As Integer)
        {|stmt1:bar|} = [if]
    End Sub
End Class
                               </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="if")

                    result.AssertLabeledSpecialSpansAre("escape", "[if]", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt1", "Me.if = [if]", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub ParameterConflictingWithSharedField(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class GooClass
    Shared goo As Integer

    Sub Blah([|$$bar|] As Integer)
        {|stmt2:goo|} = {|stmt1:bar|}
    End Sub
End Class
                               </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo")

                    result.AssertLabeledSpansAre("stmt1", "GooClass.goo = goo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "GooClass.goo = goo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub ParameterConflictingWithFieldInModule(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module GooModule
    Private goo As Integer

    Sub Blah([|$$bar|] As Integer)
        {|stmt2:goo|} = {|stmt1:bar|}
    End Sub
End Module
                               </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo")

                    result.AssertLabeledSpansAre("stmt1", "GooModule.goo = goo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "GooModule.goo = goo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub MinimalQualificationOfBaseType1(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="B")

                    result.AssertLabeledSpansAre("Resolve", "X.B", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub MinimalQualificationOfBaseType2(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="A")

                    result.AssertLabeledSpansAre("Resolve", "X.A", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub PreserveTypeCharactersForKeywordsAsIdentifiers(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Option Strict On

Class C
    Sub New
        Dim x$ = Me.{|stmt1:$$Goo|}.ToLower
    End Sub

    Function {|TypeSuffix:Goo$|}
        Return "ABC"
    End Function
End Class

                               </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Class")

                    result.AssertLabeledSpansAre("stmt1", "Class", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpecialSpansAre("TypeSuffix", "Class$", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529695")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543016")>
            <WpfTheory(Skip:="529695"), CombinatorialData>
            Public Sub RenameDoesNotBreakQuery(host As RenameTestHost)
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
                        </Workspace>, host:=host, renameTo:="GooSelect")

                    result.AssertLabeledSpansAre("escaped", "[GooSelect]", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WpfTheory(Skip:="566460")>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566460")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542349")>
            Public Sub ProperlyEscapeNewKeywordWithTypeCharacters(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Option Strict On

Class C
    Sub New
        Dim x$ = Me.{|stmt1:$$Goo$|}.ToLower
    End Sub

    Function {|Unescaped:Goo$|}
        Return "ABC"
    End Function
End Class
                               </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="New")

                    result.AssertLabeledSpansAre("Unescaped", "New$", type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt1", "Dim x$ = Me.[New].ToLower", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub AvoidDoubleEscapeAttempt(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="[true]")

                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub ReplaceAliasWithNestedGenericType(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="C")

                    result.AssertLabeledSpansAre("stmt1", "Dim x As A(Of Integer).B", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540440")>
            Public Sub RenamingFunctionWithFunctionVariableFromFunction(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

                    result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540440")>
            Public Sub RenamingFunctionWithFunctionVariableFromFunctionVariable(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

                    result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <WpfTheory(Skip:="566542")>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542999")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566542")>
            Public Sub ResolveConflictingTypeIncludedThroughModule1(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="A")

                    result.AssertLabeledSpansAre("Replacement", "N.X.A", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <WpfTheory(Skip:="566542")>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543068")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566542")>
            Public Sub ResolveConflictingTypeIncludedThroughModule2(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="B")

                    result.AssertLabeledSpansAre("Replacement", "N.X.B")
                    result.AssertLabeledSpansAre("Resolved", type:=RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543068")>
            Public Sub ResolveConflictingTypeImportedFromMultipleTypes(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Imports X
Imports Y

Module Program
    Sub Main
        {|stmt1:Goo|} = 1
    End Sub
End Module

Class X
    Public Shared [|$$Goo|]
End Class

Class Y
    Public Shared Bar
End Class
                            ]]></Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("stmt1", "X.Bar = 1", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542936")>
            Public Sub ConflictWithImplicitlyDeclaredLocal(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Option Explicit Off
Module Program
    Function [|$$Goo|]
        {|Conflict:Bar|} = 1
    End Function
End Module
                            ]]></Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542886")>
            Public Sub RenameForRangeVariableUsedInLambda(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="j")

                    result.AssertLabeledSpansAre("stmt1", "j", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543021")>
            <CombinatorialData>
            Public Sub ShouldNotCascadeToExplicitlyImplementedInterfaceMethodOfDifferentName(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Baz")

                End Using
            End Sub

            <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543021")>
            <CombinatorialData>
            Public Sub ShouldNotCascadeToImplementingMethodOfDifferentName(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Baz")

                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub RenameAttributeSuffix(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|Special:Something|}()>
Public class goo
End class

Public Class [|$$SomethingAttribute|]
	Inherits Attribute
End Class]]></Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="SpecialAttribute")

                    result.AssertLabeledSpansAre("Special", "Special", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            Public Sub RenameAttributeFromUsage(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|Special:Something|}()>
Public class goo
End class

Public Class {|Special:$$SomethingAttribute|}
	Inherits Attribute
End Class]]></Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Special")

                    result.AssertLabeledSpansAre("Special", "Special", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543488")>
            Public Sub RenameFunctionCallAfterElse(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="NewMethod1")

                    result.AssertLabeledSpansAre("stmt1", "NewMethod1", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem(11004, "DevDiv_Projects/Roslyn")>
            Public Sub RenameImplicitlyDeclaredLocal(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Option Explicit Off
 
Module Program
    Sub Main(args As String())
        {|stmt1:$$goo|} = 23
        {|stmt2:goo|} = 42
    End Sub
End Module
                           </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="barbaz")

                    result.AssertLabeledSpansAre("stmt1", "barbaz", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt2", "barbaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem(11004, "DevDiv_Projects/Roslyn")>
            Public Sub RenameFieldToConflictWithImplicitlyDeclaredLocal(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Option Explicit Off
 
Module Program
    Dim [|$$bar|] As Object
 
    Sub Main(args As String())
        {|stmt1_2:goo|} = {|stmt1:bar|}
        {|stmt2:goo|} = 42
    End Sub
End Module

                           </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="goo")

                    result.AssertLabeledSpansAre("stmt1", "goo", type:=RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("stmt1_2", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("stmt2", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543420")>
            Public Sub RenameParameterOfEvent(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="barbaz")

                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543587")>
            Public Sub RenameLocalInMethodMissingParameterList(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="barbaz")

                    result.AssertLabeledSpansAre("stmt1", "barbaz", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542649")>
            Public Sub QualifyTypeWithGlobalWhenConflicting(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="A")

                    result.AssertLabeledSpansAre("Resolve", "Global.A", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542322")>
            Public Sub QualifyFieldInReDimStatement(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Module Preserve
    Sub Main
        Dim Bar
        ReDim {|stmt1:Goo|}(0)
    End Sub
 
    Property [|$$Goo|]
End Module
                        </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("stmt1", "ReDim [Preserve].Bar(0)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566542")>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545604")>
            <WpfTheory, CombinatorialData>
            Public Sub QualifyTypeNameInImports(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="X")

                    result.AssertLabeledSpansAre("Resolve", "M.X", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameNewOverload(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports System
Module Program
    Sub Main()
        {|ResolvedNonReference:Goo|}(Sub(x) x.{|Resolve:Old|}())
    End Sub
    Sub Goo(x As Action(Of I))
    End Sub
    Sub Goo(x As Action(Of C))
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
                    </Workspace>, host:=host, renameTo:="New")

                    result.AssertLabeledSpecialSpansAre("Escape", "[New]", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Resolve", "Goo(Sub(x) x.New())", RelatedLocationType.ResolvedReferenceConflict)
                    result.AssertLabeledSpansAre("ResolvedNonReference", "Goo(Sub(x) x.New())", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameAttributeRequiringReducedNameToResolveConflict(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="ZAttribute")

                    result.AssertLabeledSpecialSpansAre("resolve", "Z", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameEvent(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Y")

                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameInterfaceImplementation(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb">
Imports System
Interface I
    Sub Goo(Optional x As Integer = 0)
End Interface
Class C
    Implements I
    Shared Sub Main()
        DirectCast(New C(), I).Goo()
    End Sub
    Private Sub [|$$I_Goo|](Optional x As Integer = 0) Implements I.Goo
        Console.WriteLine("test")
    End Sub
End Class
                        </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameAttributeConflictWithNamespace(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="B")

                    result.AssertLabeledSpansAre("Resolve", "X.B", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameREMToUnicodeREM(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:=text)

                    result.AssertLabeledSpecialSpansAre("Resolve", compareText, RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameImports(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Attribute")

                    result.AssertLabeledSpansAre("Resolve1", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Resolve2", "Attribute", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578105")>
            Public Sub Bug578105_VBRenamingPartialMethodDifferentCasing(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Class Goo    
    Partial Private Sub [|Goo|]()
    End Sub

    Private Sub [|$$goo|]()
    End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Baz")

                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/588142")>
            Public Sub Bug588142_SimplifyAttributeUsageCanAlwaysEscapeInVB(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="RemAttribute")

                    result.AssertLabeledSpansAre("escaped", "[Rem]", RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/588038")>
            Public Sub Bug588142_RenameAttributeToAttribute(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|unreduced:Goo|}>
Class [|$$GooAttribute|] ' Rename Goo to Attribute
    Inherits {|resolved:Attribute|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Attribute")

                    result.AssertLabeledSpansAre("unreduced", "Attribute", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("resolved", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576573")>
            Public Sub Bug576573_ConflictAttributeWithNamespace(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BAttribute")

                    result.AssertLabeledSpansAre("resolved", "X.B", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603368")>
            Public Sub Bug603368_ConflictAttributeWithNamespaceCaseInsensitive(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BATTRIBUTE")

                    result.AssertLabeledSpansAre("resolved", "X.B", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603367")>
            Public Sub Bug603367_ConflictAttributeWithNamespaceCaseInsensitive2(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<{|resolved:Goo|}>
Module M
    Class GooAttribute
        Inherits Attribute
    End Class
End Module
 
Class [|$$X|] ' Rename X to GOOATTRIBUTE
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="GOOATTRIBUTE")

                    result.AssertLabeledSpansAre("resolved", "M.Goo", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603276")>
            Public Sub Bug603276_ConflictAttributeWithNamespaceCaseInsensitive3(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Imports System

<[|Goo|]>
Class [|$$Goo|] ' Rename Goo to ATTRIBUTE
    Inherits {|resolved:Attribute|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="ATTRIBUTE")

                    result.AssertLabeledSpansAre("resolved", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529712")>
            Public Sub Bug529712_ConflictNamespaceWithModuleName_1(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                            <Document FilePath="Test.vb"><![CDATA[
Module Program
    Sub Main()
        N.{|resolved:Goo|}()
    End Sub
End Module
 
Namespace N
    Namespace [|$$Y|] ' Rename Y to Goo
    End Namespace
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("resolved", "N.X.Goo()", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529837")>
            Public Sub Bug529837_ResolveConflictByOmittingModuleName(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="C")

                    result.AssertLabeledSpansAre("resolved", "X.C", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529989")>
            Public Sub Bug529989_RenameCSharpIdentifierToInvalidVBIdentifier(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="B\u0061r")

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("invalid", "B\u0061r", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameModuleBetweenAssembly(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <ProjectReference>Project2</ProjectReference>
                            <Document>
Imports System
Module Program
    Sub Main(args As String())
        Dim {|Stmt1:$$Bar|} = Sub(x) Console.Write(x)
        Call {|Resolve:Goo|}()
        {|Stmt2:Bar|}(1)
    End Sub
End Module                   
                         </Document>
                        </Project>
                        <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                            <Document>
Public Module M
    Sub Goo()
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("Stmt1", "Goo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Stmt2", "Goo", RelatedLocationType.NoConflict)
                    result.AssertLabeledSpansAre("Resolve", "Call M.Goo()", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameModuleClassConflict(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Namespace N
    Module M
        Class C
            Shared Sub Goo()

            End Sub
        End Class
    End Module
    Class [|$$D|]
        Shared Sub Goo()

        End Sub
    End Class
    Module Program
        Sub Main()
            {|Resolve:C|}.{|Resolve:Goo|}()    
            {|Stmt1:D|}.Goo()
        End Sub
    End Module 
End Namespace
                       
                             </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="C")

                    result.AssertLabeledSpansAre("Resolve", "M.C.Goo()", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Stmt1", "C", RelatedLocationType.NoConflict)

                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameModuleNamespaceNested(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Namespace N
    Namespace M
        Module K
            Sub Goo()

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
        N.M.{|Resolve1:Goo|}()
        N.M.{|Resolve2:Bar|}()
    End Sub
End Module                       
                             </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("Resolve1", "N.M.K.Goo()", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("Resolve2", "N.M.L.Goo()", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            Public Sub RenameModuleConflictWithInterface(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                            <Document>
Imports System
Interface M
    Sub goo(ByVal x As Integer)
End Interface
Namespace N
    Module [|$$K|]
        Sub goo(ByVal x As Integer)

        End Sub
    End Module
    Class C
        Implements {|Resolve:M|}
        Public Sub goo(x As Integer) Implements {|Resolve:M|}.goo
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace                             </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="M")

                    result.AssertLabeledSpansAre("Resolve", "Global.M", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/628700")>
            Public Sub RenameModuleConflictWithLocal(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="x")

                    result.AssertLabeledSpansAre("Resolve", "N.M.x", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633180")>
            Public Sub VB_DetectOverLoadResolutionChangesInEnclosingInvocations(host As RenameTestHost)
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
    ' Rename Ex To Goo
    &lt;Extension()>
    Public Sub [|$$Ex|](x As Integer)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                    result.AssertLabeledSpansAre("resolved", "Outer(Sub(y As String) Inner(Sub(x) x.Ex(), y), 0)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673562"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
            Public Sub RenameNamespaceConflictsAndResolves(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="NN")

                    result.AssertLabeledSpansAre("resolve1", "Global.NN.C", RelatedLocationType.ResolvedNonReferenceConflict)
                    result.AssertLabeledSpansAre("resolve2", "Global.NN", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673667")>
            Public Sub RenameUnnecessaryExpansion(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="N")

                    result.AssertLabeledSpansAre("resolve", "Global.N", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645152")>
            Public Sub AdjustTriviaForExtensionMethodRewrite(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Test.cs">
Imports System.Runtime.CompilerServices
 
Class C
Sub Bar(tag As Integer)
        Me.{|resolve:Goo|}(1).{|resolve:Goo|}(2)
    End Sub
End Class
 
Module E
    &lt;Extension&gt;
    Public Function [|$$Goo|](x As C, tag As Integer) As C
        Return x
    End Function
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Bar")

                    result.AssertLabeledSpansAre("resolve", "E.Bar(E.Bar(Me,1),2)", RelatedLocationType.ResolvedReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569103")>
            Public Sub RenameCrefWithConflict(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports F = N
Namespace N
    Interface I
        Sub Goo()
    End Interface
End Namespace

Class C
	Private Class E
        Implements {|Resolve:F|}.I
        ''' <summary>
        ''' This is a function <see cref="{|Resolve:F|}.I.Goo"/>
        ''' </summary>
        Public Sub Goo() Implements {|Resolve:F|}.I.Goo
        End Sub
    End Class
	Private Class [|$$K|]
	End Class
End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="F")

                    result.AssertLabeledSpansAre("Resolve", "N", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768910")>
            Public Sub RenameInCrefPreservesWhitespaceTrivia(host As RenameTestHost)
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
        Shared Sub [|$$goo|]()    ' Rename goo to D 
        End Sub
    End Class
    Public Class D
    End Class
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="D")

                    result.AssertLabeledSpansAre("Resolve", "A.D", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016652")>
            Public Sub VB_ConflictBetweenTypeNamesInTypeConstraintSyntax(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="ISymbol")

                    result.AssertLabeledSpansAre("DeclConflict", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("unresolved1", type:=RelatedLocationType.UnresolvedConflict)
                    result.AssertLabeledSpansAre("unresolved2", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/905")>
            Public Sub RenamingCompilerGeneratedPropertyBackingField_InvokeFromProperty(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Y")

                    result.AssertLabeledSpecialSpansAre("backingfield", "_Y", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/905")>
            Public Sub RenamingCompilerGeneratedPropertyBackingField_IntroduceConflict(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Y")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <WpfTheory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/905")>
            Public Sub RenamingCompilerGeneratedPropertyBackingField_InvokableFromBackingFieldReference(host As RenameTestHost)
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
                    </Workspace>, host)

                    AssertTokenRenamable(workspace)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/1193")>
            Public Sub MemberQualificationInNameOfUsesTypeName_StaticReferencingInstance(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="zoo")

                    result.AssertLabeledSpansAre("ref", "Dim x = NameOf(C.zoo)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/1193")>
            Public Sub MemberQualificationInNameOfUsesTypeName_InstanceReferencingStatic(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="zoo")

                    result.AssertLabeledSpansAre("ref", "Dim x = NameOf(C.zoo)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/1193")>
            Public Sub MemberQualificationInNameOfUsesTypeName_InstanceReferencingInstance(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="zoo")

                    result.AssertLabeledSpansAre("ref", "Dim x = NameOf(C.zoo)", RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            Public Sub TestConflictBetweenClassAndInterface1(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="C")

                    result.AssertLabeledSpansAre("conflict", "C", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            Public Sub TestConflictBetweenClassAndInterface2(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="I")

                    result.AssertLabeledSpansAre("conflict", "I", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            Public Sub TestConflictBetweenClassAndNamespace1(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="N")

                    result.AssertLabeledSpansAre("conflict", "N", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            Public Sub TestConflictBetweenClassAndNamespace2(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="C")

                    result.AssertLabeledSpansAre("conflict", "C", RelatedLocationType.UnresolvableConflict)
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027506")>
            Public Sub TestNoConflictBetweenTwoNamespaces(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="N2")
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/1195")>
            Public Sub NameOfReferenceNoConflict(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Test")
                End Using
            End Sub

            <Theory, CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/1195")>
            Public Sub NameOfReferenceWithConflict(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="Test")

                    result.AssertLabeledSpansAre("conflict", "Test", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/1031")>
            <CombinatorialData>
            Public Sub InvalidNamesDoNotCauseCrash_IntroduceQualifiedName(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document FilePath="Test.cs"><![CDATA[
Class {|conflict:C$$|}
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="C.D")

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("conflict", "C.D", RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/1031")>
            <CombinatorialData>
            Public Sub InvalidNamesDoNotCauseCrash_AccidentallyPasteLotsOfCode(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:=renameTo)

                    result.AssertReplacementTextInvalid()
                    result.AssertLabeledSpansAre("conflict", renameTo, RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/7440")>
            Public Sub RenameTypeParameterInPartialClass(host As RenameTestHost)
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
                        </Workspace>, host:=host, renameTo:="T2")
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/7440")>
            Public Sub RenameMethodToConflictWithTypeParameter(host As RenameTestHost)
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
                        </Workspace>, host:=host, renameTo:="T")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/16576")>
            Public Sub RenameParameterizedPropertyResolvedConflict(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document><![CDATA[
Public Class C
    Public ReadOnly Property P(a As Object) As Int32
        Get
            Return 2
        End Get
    End Property
    Public ReadOnly Property [|$$P2|](a As String) As Int32
        Get
            Return {|Conflict0:P|}("")
        End Get
    End Property
End Class
]]>
                                </Document>
                            </Project>
                        </Workspace>, host:=host, renameTo:="P")

                    result.AssertLabeledSpansAre("Conflict0", replacement:="Return P(CObj(""""))", type:=RelatedLocationType.ResolvedNonReferenceConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/16576")>
            Public Sub RenameParameterizedPropertyUnresolvedConflict(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document><![CDATA[
Public Class C
    Public ReadOnly Property {|Conflict:P|}(a As String) As Int32
        Get
            Return 2
        End Get
    End Property
    Public ReadOnly Property [|$$P2|](a As String) As Int32
        Get
            Return 3
        End Get
    End Property
End Class
]]>
                                </Document>
                            </Project>
                        </Workspace>, host:=host, renameTo:="P")

                    result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/10469")>
            Public Sub RenameTypeToCurrent(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document>
Class {|current:$$C|}
End Class
                                </Document>
                            </Project>
                        </Workspace>, host:=host, renameTo:="Current")

                    result.AssertLabeledSpansAre("current", type:=RelatedLocationType.NoConflict)
                End Using
            End Sub

            <Theory>
            <CombinatorialData>
            <WorkItem("https://github.com/dotnet/roslyn/issues/32086")>
            Public Sub InvalidControlVariableInForLoopDoNotCrash(host As RenameTestHost)
                Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document><![CDATA[
Module Program
    Sub Main()
        Dim [|$$val|] As Integer = 10
        For (Int() i = 0; i < val; i++)
    End Sub
End Module
                            ]]></Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="v")
                End Using
            End Sub
        End Class
    End Class
End Namespace
