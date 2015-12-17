' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.VisualBasic
    Public Class DeclarationConflictTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenFields()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module FooModule
    Dim [|$$foo|] As Integer
    Dim {|Conflict:bar|} As Integer
End Module
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenFieldAndMethod()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module FooModule
    Dim [|$$foo|] As Integer
    Sub {|Conflict:bar|}()
End Module
                           </Document>
                    </Project>
                </Workspace>,
               renameTo:="bar")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenTwoMethodsWithSameSignature()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module FooModule
    Sub [|$$foo|]()
    End Sub

    Sub {|Conflict:bar|}()
    End Sub
End Module
                            </Document>
                    </Project>
                </Workspace>, renameTo:="bar")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenTwoParameters()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module FooModule
    Sub f([|$$foo|] As Integer, {|Conflict:bar|} As Integer)
    End Sub
End Module
                               </Document>
                    </Project>
                </Workspace>, renameTo:="bar")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictBetweenMethodsWithDifferentSignatures()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module FooModule
    Sub [|$$foo|]()
    End Sub

    Sub bar(parameter As Integer)
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="bar")


            End Using
        End Sub

        <Fact>
        <WorkItem(543245)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenTwoLocals()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="j")

                result.AssertLabeledSpansAre("stmt1", "j", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543245)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLocalAndParameter()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="args")

                result.AssertLabeledSpansAre("stmt1", "args", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(545859)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenQueryVariableAndParameter()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="args")

                result.AssertLabeledSpansAre("stmt1", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(545859)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenTwoQueryVariables()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="x")

                result.AssertLabeledSpansAre("stmt1", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543654)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLambdaParametersInsideMethod()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="c")

                result.AssertLabeledSpansAre("stmt1", "c", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543654)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLambdaParametersInFieldInitializer()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System

Module M1
    Dim y = Sub({|Conflict:c|}) Call (Sub({|stmt:$$b|}) Exit Sub)(c)
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="c")

                result.AssertLabeledSpansAre("stmt", "c", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543654)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictBetweenLambdaParameterAndField()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System

Module M1
    Dim y = Sub({|fieldinit:$$c|}) Exit Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("fieldinit", "y", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543407)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenLabels()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class Program
    Sub Main()
{|Conflict:Foo|}:
[|$$Bar|]:

        Dim f = Sub()
Foo:
                End Sub
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="Foo")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543308)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenMethodsDifferingByByRef()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="a")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543308)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenMethodsDifferingByOptional()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="a")


                result.AssertLabeledSpansAre("Conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(543308)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NoConflictBetweenMethodsDifferingByArity()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="a")


            End Using
        End Sub

        <Fact>
        <WorkItem(546902)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenImplicitlyDeclaredLocalAndNamespace()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Microsoft")

                result.AssertLabeledSpansAre("Conflict1", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("Conflict2", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529556)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenImplicitlyDeclaredLocalAndAndGlobalFunction()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Write")

                result.AssertLabeledSpansAre("Conflict", "Write", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(542217)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenAliases()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="A")

                result.AssertLabeledSpansAre("Conflict", "A", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530125)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenImplicitVariableAndClass()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="X")

                result.AssertLabeledSpansAre("conflict", "X", RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530038)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenEquallyNamedAlias()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Something")


                result.AssertLabeledSpansAre("conflict1", "Something", RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("conflict2", "NS1", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("conflict3", "Something", RelatedLocationType.ResolvedNonReferenceConflict)
                result.AssertLabeledSpansAre("noconflict", "Something", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(610120)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenEquallyNamedPropertyAndItsParameter_1()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(610120)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenEquallyNamedPropertyAndItsParameter_2()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Y")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(610120)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictBetweenEquallyNamedPropertyAndItsParameter_3()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="X")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(608198), WorkItem(798375)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictInFieldInitializerOfFieldAndModuleNameResolvedThroughFullQualification()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System
Module [|$$M|] ' Rename M to X
    Dim x As Action = Sub() Console.WriteLine({|stmt1:M|}.x)
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="X")


                result.AssertLabeledSpansAre("stmt1", "Console.WriteLine(Global.X.x)", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(528706)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForForEachLoopVariableNotBindingToTypeAnyMore()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="M")


                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530476)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Sub Main
            For Each {|ctrlvar:foo|} In {1, 2, 3}
                Dim y As Integer = (From {|conflict:g|} In {{|broken:foo|}} Select g).First()
                Console.WriteLine({|stmt:$$foo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="g")

                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("broken", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530476)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Sub Main
            For Each {|ctrlvar:foo|} As Integer In {1, 2, 3}
                Dim y As Integer = (From {|conflict:g|} In {{|broken:foo|}} Select g).First()
                Console.WriteLine({|stmt:$$foo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="g")

                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("broken", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530476)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_3()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Sub Main
            Dim {|stmt1:foo|} as Integer
            For Each {|ctrlvar:foo|} In {1, 2, 3}
                Dim y As Integer = (From {|conflict:g|} In {{|broken:foo|}} Select g).First()
                Console.WriteLine({|stmt2:$$foo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="g")

                result.AssertLabeledSpansAre("stmt1", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("conflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("broken", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530476)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForForEachLoopVariableAndRangeVariable_4()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Option Infer On

Imports System.Linq
 
Namespace X
    Module Program
        Public [|foo|] as Integer
        Sub Main
            For Each Program.{|ctrlvar:foo|} In {1, 2, 3}
                Dim y As Integer = (From g In {{|query:foo|}} Select g).First()
                Console.WriteLine({|stmt:$$foo|})
            Next
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>, renameTo:="g")


                result.AssertLabeledSpansAre("ctrlvar", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("query", "g", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "g", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530476)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForUsingVariableAndRangeVariable_1()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="c")

                result.AssertLabeledSpansAre("usingstmt", "c", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("query", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "c", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(530476)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForUsingVariableAndRangeVariable_2()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="v4")

                result.AssertLabeledSpansAre("usingstmt", "v4", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("query", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "v4", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <WpfFact(Skip:="657210")>
        <WorkItem(653311)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForUsingVariableAndRangeVariable_3()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="d")

                result.AssertLabeledSpansAre("usingstmt", "d", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("query", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt", "d", type:=RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictForCatchVariable_1()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="y")

                result.AssertLabeledSpansAre("catchstmt", "y", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529986)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictBetweenTypeParametersInTypeDeclaration()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Foo(Of {|declconflict:T|} as {New}, [|$$U|])
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="T")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529986)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictBetweenTypeParametersInMethodDeclaration_1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Foo
    Public Sub M(Of {|declconflict:T|} as {New}, [|$$U|])()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="T")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529986)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictBetweenTypeParametersInMethodDeclaration_2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Foo
    Public Sub M(Of {|declconflict:[T]|} as {New}, [|$$U|])()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="t")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529986)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictBetweenTypeParameterAndMember_1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Foo(Of {|declconflict:[T]|})
    Public Sub [|$$M|]()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="t")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529986)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictBetweenTypeParameterAndMember_2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Foo(Of {|declconflict:[T]|})
    Public [|$$M|] as Integer = 23
End Class
                        </Document>
                    </Project>
                </Workspace>, renameTo:="t")


                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(658437)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_ConflictBetweenEscapedForEachControlVariableAndQueryRangeVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System.Linq

Module Program

    Sub Main(args As String())
        For Each {|stmt1:foo|} In {1, 2, 3}
            Dim x As Integer = (From {|declconflict:g|} In {{|stmt3:foo|}} Select g).First()
            Console.WriteLine({|stmt2:$$foo|})
        Next

    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, renameTo:="[g]")

                result.AssertLabeledSpansAre("stmt1", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt2", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("stmt3", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(658801)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_OverridingImplicitlyUsedMethod()
            Using result = RenameEngineResult.Create(
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

    Public Sub {|possibleImplicitConflict:$$Foo|}() ' Rename Foo to MoveNext
    End Sub
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="movenext")

                result.AssertLabeledSpansAre("possibleImplicitConflict", type:=RelatedLocationType.UnresolvedConflict)
            End Using
        End Sub

        <WorkItem(682669)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_OverridingImplicitlyUsedMethod_1()
            Using result = RenameEngineResult.Create(
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

    Public Overloads Sub [|$$Foo|](of T)() ' Rename Foo to MoveNext
    End Sub
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="movenext")


            End Using
        End Sub

        <WorkItem(682669)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VB_OverridingImplicitlyUsedMethod_2()
            Using result = RenameEngineResult.Create(
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

    Public Sub [|$$Foo|]() ' Rename Foo to MoveNext
    End Sub
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="movenext")


            End Using
        End Sub

        <WorkItem(851604)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ConflictInsideSimpleArgument()
            Using result = RenameEngineResult.Create(
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
                </Workspace>, renameTo:="Method")

                result.AssertLabeledSpansAre("first", "Method", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("second", "C.Method", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
