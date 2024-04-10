' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class DeclarationConflictTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenFields(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module GooModule
    Dim [|$$goo|] As Integer
    Dim {|Conflict:bar|} As Integer
End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenFieldAndMethod(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module GooModule
    Dim [|$$goo|] As Integer
    Sub {|Conflict:bar|}()
End Module
                           </Document>
                    </Project>
                </Workspace>, host:=host,
               renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenTwoMethodsWithSameSignature(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module GooModule
    Sub [|$$goo|]()
    End Sub

    Sub {|Conflict:bar|}()
    End Sub
End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConflictBetweenTwoParameters(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module GooModule
    Sub f([|$$goo|] As Integer, {|Conflict:bar|} As Integer)
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="bar")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub NoConflictBetweenMethodsWithDifferentSignatures(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module GooModule
    Sub [|$$goo|]()
    End Sub

    Sub bar(parameter As Integer)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="bar")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543245")>
        <CombinatorialData>
        Public Sub ConflictBetweenTwoLocals(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main(args As String())
        Dim {|stmt1:$$i|} = 1
        Dim {|Conflict:j|} = 2
    End Sub
End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543245")>
        <CombinatorialData>
        Public Sub ConflictBetweenLocalAndParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main({|Conflict:args|} As String())
        Dim {|stmt1:$$i|} = 1
    End Sub
End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="args")

                result.AssertLabeledSpansAre("stmt1", "args", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545859")>
        <CombinatorialData>
        Public Sub ConflictBetweenQueryVariableAndParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main({|Conflict:args|} As String())
        Dim z = From {|stmt1:$$x|} In args
    End Sub
End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="args")

                result.AssertLabeledSpansAre("stmt1", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545859")>
        <CombinatorialData>
        Public Sub ConflictBetweenTwoQueryVariables(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main(args As String())
        Dim z = From {|Conflict:x|} In args
                From {|stmt1:$$y|} In args
    End Sub
End Module
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="x")

                result.AssertLabeledSpansAre("stmt1", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543654")>
        <CombinatorialData>
        Public Sub ConflictBetweenLambdaParametersInsideMethod(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System

Module M1
    Sub Main()
        Dim y = Sub({|Conflict:c|}) Call (Sub(a, {|stmt1:$$b|}) Exit Sub)(c)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="c")

                result.AssertLabeledSpansAre("stmt1", "c", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543654")>
        <CombinatorialData>
        Public Sub ConflictBetweenLambdaParametersInFieldInitializer(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System

Module M1
    Dim y = Sub({|Conflict:c|}) Call (Sub({|stmt:$$b|}) Exit Sub)(c)
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="c")

                result.AssertLabeledSpansAre("stmt", "c", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543654")>
        <CombinatorialData>
        Public Sub NoConflictBetweenLambdaParameterAndField(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System

Module M1
    Dim y = Sub({|fieldinit:$$c|}) Exit Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("fieldinit", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543407")>
        <CombinatorialData>
        Public Sub ConflictBetweenLabels(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class Program
    Sub Main()
{|Conflict:Goo|}:
[|$$Bar|]:

        Dim f = Sub()
Goo:
                End Sub
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Goo")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543308")>
        <CombinatorialData>
        Public Sub ConflictBetweenMethodsDifferingByByRef(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
 
Module Program
    Sub {|Conflict:a|}(x As Integer)
    End Sub
 
    Sub [|$$c|](ByRef x As Integer)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="a")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543308")>
        <CombinatorialData>
        Public Sub ConflictBetweenMethodsDifferingByOptional(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
 
Module Program
    Sub {|Conflict:a|}(x As Integer)
    End Sub
 
    Sub [|$$d|](x As Integer, Optional y As Integer = 0)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="a")

                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543308")>
        <CombinatorialData>
        Public Sub NoConflictBetweenMethodsDifferingByArity(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
 
Module Program
    Sub a(Of T)(x As Integer)
    End Sub
 
    Sub [|$$d|](x As Integer, Optional y As Integer = 0)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="a")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546902")>
        <CombinatorialData>
        Public Sub ConflictBetweenImplicitlyDeclaredLocalAndNamespace(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Explicit Off
Module Program
    Sub Main()
        __ = {|Conflict1:$$Google|}
        {|Conflict2:Google|} = __
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Microsoft")

                result.AssertLabeledSpansAre("Conflict1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("Conflict2", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529556")>
        <CombinatorialData>
        Public Sub ConflictBetweenImplicitlyDeclaredLocalAndAndGlobalFunction(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports Microsoft.VisualBasic

Module Module1
    Sub Main()
        Dim a = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
        Dim q = From i In a
                Where i Mod 2 = 0
                Select Function() i * i
        For Each {|Conflict:$$sq|} In q
            Console.Write({|Conflict:sq|}())
        Next
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Write")

                result.AssertLabeledSpansAre("Conflict", "Write", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542217")>
        <CombinatorialData>
        Public Sub ConflictBetweenAliases(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports A = X.Something
Imports {|Conflict:$$B|} = X.SomethingElse
 
Namespace X
    Class Something
    End Class

    Class SomethingElse
    End Class
End Namespace

                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="A")

                result.AssertLabeledSpansAre("Conflict", "A", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530125")>
        <CombinatorialData>
        Public Sub ConflictBetweenImplicitVariableAndClass(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Explicit Off
 
Class X
End Class
 
Module M
    Sub Main()
        {|conflict:$$Y|} = 1
    End Sub
End Module


                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("conflict", "X", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530038")>
        <CombinatorialData>
        Public Sub ConflictBetweenEquallyNamedAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports [|$$A|] = NS1.Something
Imports {|conflict1:Something|} = NS1
Namespace NS1
    Class Something
        Public Something()
    End Class
End Namespace

Class Program
    Dim a As {|noconflict:A|}
    Dim q As {|conflict2:Something|}.{|conflict3:Something|}
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Something")

                result.AssertLabeledSpansAre("conflict1", "Something", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict2", "NS1", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("conflict3", "Something", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("noconflict", "Something", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610120")>
        <CombinatorialData>
        Public Sub ConflictBetweenEquallyNamedPropertyAndItsParameter_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class A
    Public Property [|$$X|]({|declconflict:Y|} As Integer) As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610120")>
        <CombinatorialData>
        Public Sub ConflictBetweenEquallyNamedPropertyAndItsParameter_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class A
    Public Overridable Property [|X|]({|declconflict:Y|} As Integer) As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Class

Public Class B
    Inherits A

    Public Overrides Property [|$$X|]({|declconflict:y|} As Integer) As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Class

Public Class C
    Inherits A

    Public Overrides Property [|X|]({|declconflict:y|} As Integer) As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Y")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610120")>
        <CombinatorialData>
        Public Sub ConflictBetweenEquallyNamedPropertyAndItsParameter_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class A
    Public Overridable Property {|declconflict:X|}([|$$Y|] As Integer) As Integer
        Get
            Return 0
        End Get

        Set
        End Set
    End Property
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798375")>
        <CombinatorialData>
        Public Sub VB_ConflictInFieldInitializerOfFieldAndModuleNameResolvedThroughFullQualification(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System
Module [|$$M|] ' Rename M to X
    Dim x As Action = Sub() Console.WriteLine({|stmt1:M|}.x)
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("stmt1", "Console.WriteLine(Global.X.x)", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528706")>
        <CombinatorialData>
        Public Sub VB_ConflictForForEachLoopVariableNotBindingToTypeAnyMore(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On
 
Namespace X
    Module Program
        Sub Main
            For Each {|conflict:x|} In ""
            Next
        End Sub
    End Module
End Namespace
 
Namespace X
    Class [|$$X|] ' Rename X to M
    End Class
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="M")

                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530476")>
        <CombinatorialData>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Sub Main
            For Each {|ctrlvar:goo|} In {1, 2, 3}
                Dim y As Integer = (From {|conflict:g|} In {{|broken:goo|}} Select g).First()
                Console.WriteLine({|stmt:$$goo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="g")

                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("broken", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530476")>
        <CombinatorialData>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Sub Main
            For Each {|ctrlvar:goo|} As Integer In {1, 2, 3}
                Dim y As Integer = (From {|conflict:g|} In {{|broken:goo|}} Select g).First()
                Console.WriteLine({|stmt:$$goo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="g")

                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("broken", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530476")>
        <CombinatorialData>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Sub Main
            Dim {|stmt1:goo|} as Integer
            For Each {|ctrlvar:goo|} In {1, 2, 3}
                Dim y As Integer = (From {|conflict:g|} In {{|broken:goo|}} Select g).First()
                Console.WriteLine({|stmt2:$$goo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="g")

                result.AssertLabeledSpansAre("stmt1", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("broken", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530476")>
        <CombinatorialData>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_4(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Public [|goo|] as Integer
        Sub Main
            For Each Program.{|ctrlvar:goo|} In {1, 2, 3}
                Dim y As Integer = (From g In {{|query:goo|}} Select g).First()
                Console.WriteLine({|stmt:$$goo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="g")

                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("query", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530476")>
        <CombinatorialData>
        Public Sub VB_ConflictForUsingVariableAndRangeVariable_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq

Namespace X
    Module Program
        Sub Main
            Using {|usingstmt:v1|} = new Object, v2 as Object = new Object(), v3, v4 as new Object()
                Dim o As Object = (From {|declconflict:c|} In {{|query:v1|}} Select c).First()
                Console.WriteLine({|stmt:$$v1|})
            End Using
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="c")

                result.AssertLabeledSpansAre("usingstmt", "c", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("query", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "c", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530476")>
        <CombinatorialData>
        Public Sub VB_ConflictForUsingVariableAndRangeVariable_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Namespace X
    Module Program
        Sub Main
            Using {|usingstmt:v3|}, {|declconflict:v4|} as new Object()
                Dim o As Object = (From c In {{|query:v3|}} Select c).First()
                Console.WriteLine({|stmt:$$v3|})
            End Using
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="v4")

                result.AssertLabeledSpansAre("usingstmt", "v4", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("query", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "v4", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfTheory(Skip:="657210")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653311")>
        <CombinatorialData>
        Public Sub VB_ConflictForUsingVariableAndRangeVariable_3(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On
Namespace X
    Module Program
        Sub Main
            Using {|usingstmt:v3|} as new Object()
                Dim o As Object = (From c In {{|query:v3|}} Let {|declconflict:d|} = c Select {|declconflict:d|}).First()
                Console.WriteLine({|stmt:$$v3|})
            End Using
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="d")

                result.AssertLabeledSpansAre("usingstmt", "d", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("query", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "d", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub VB_ConflictForCatchVariable_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Namespace X
    Module Program
        Sub Main
            Try 
            Catch {|catchstmt:$$x|} as Exception
                dim {|declconflict:y|} = 23
            End Try
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("catchstmt", "y", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub VB_ConflictBetweenTypeParametersInTypeDeclaration(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Goo(Of {|declconflict:T|} as {New}, [|$$U|])
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="T")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub VB_ConflictBetweenTypeParametersInMethodDeclaration_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Goo
    Public Sub M(Of {|declconflict:T|} as {New}, [|$$U|])()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="T")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub VB_ConflictBetweenTypeParametersInMethodDeclaration_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Goo
    Public Sub M(Of {|declconflict:[T]|} as {New}, [|$$U|])()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="t")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub VB_ConflictBetweenTypeParameterAndMember_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Goo(Of {|declconflict:[T]|})
    Public Sub [|$$M|]()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="t")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529986")>
        <CombinatorialData>
        Public Sub VB_ConflictBetweenTypeParameterAndMember_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Goo(Of {|declconflict:[T]|})
    Public [|$$M|] as Integer = 23
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="t")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658437")>
        <CombinatorialData>
        Public Sub VB_ConflictBetweenEscapedForEachControlVariableAndQueryRangeVariable(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System.Linq

Module Program

    Sub Main(args As String())
        For Each {|stmt1:goo|} In {1, 2, 3}
            Dim x As Integer = (From {|declconflict:g|} In {{|stmt3:goo|}} Select g).First()
            Console.WriteLine({|stmt2:$$goo|})
        Next

    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="[g]")

                result.AssertLabeledSpansAre("stmt1", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt3", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658801")>
        <CombinatorialData>
        Public Sub VB_OverridingImplicitlyUsedMethod(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Option Infer On

Imports System

Class A
    Public Property current As Integer

    Public Function MOVENext() As Boolean
        Return False
    End Function

    Public Function GetEnumerator() As C
        Return Me
    End Function
End Class

Class C
    Inherits A

    Shared Sub Main()
        For Each x In New C()
        Next
    End Sub

    Public Sub {|possibleImplicitConflict:$$Goo|}() ' Rename Goo to MoveNext
    End Sub
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="movenext")

                result.AssertLabeledSpansAre("possibleImplicitConflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682669")>
        <CombinatorialData>
        Public Sub VB_OverridingImplicitlyUsedMethod_1(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Option Infer On

Imports System

Class A
    Public Property current As Integer

    Public Function MOVENext() As Boolean
        Return False
    End Function

    Public Function GetEnumerator() As C
        Return Me
    End Function
End Class

Class C
    Inherits A

    Shared Sub Main()
        For Each x In New C()
        Next
    End Sub

    Public Overloads Sub [|$$Goo|](of T)() ' Rename Goo to MoveNext
    End Sub
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="movenext")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682669")>
        <CombinatorialData>
        Public Sub VB_OverridingImplicitlyUsedMethod_2(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Option Infer On

Imports System

Class A
    Public Property current As Integer

    Public Function MOVENext(of T)() As Boolean
        Return False
    End Function

    Public Function GetEnumerator() As C
        Return Me
    End Function
End Class

Class C
    Inherits A

    Shared Sub Main()
    End Sub

    Public Sub [|$$Goo|]() ' Rename Goo to MoveNext
    End Sub
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="movenext")

            End Using
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/851604")>
        Public Sub ConflictInsideSimpleArgument(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System.ComponentModel
Imports System.Reflection
Class C
    Const {|first:$$M|} As MemberTypes = MemberTypes.Method
    Delegate Sub D(&lt;DefaultValue({|second:M|})> x As Object);
End Class
                            </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="Method")

                result.AssertLabeledSpansAre("first", "Method", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("second", "C.Method", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18566")>
        Public Sub ParameterInPartialMethodDefinitionConflictingWithLocalInPartialMethodImplementation(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Partial Private Sub M({|parameter0:$$x|} As Integer)
    End Sub
End Class
                        </Document>
                        <Document>
Partial Class C
    Private Sub M({|parameter1:x|} As Integer)
        Dim {|local0:y|} = 1
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="y")

                result.AssertLabeledSpansAre("parameter0", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("parameter1", "y", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("local0", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/941271")>
        Public Sub AsNewClauseSpeculationResolvesConflicts(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Public Class Main
                                    Private T As {|class0:$$Test|} = Nothing
                                End Class

                                Public Class {|class1:Test|}
                                    Private Rnd As New {|classConflict:Random|}
                                End Class
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="Random")

                result.AssertLabeledSpansAre("class0", "Random", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("class1", "Random", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("classConflict", "System.Random", type:=RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
