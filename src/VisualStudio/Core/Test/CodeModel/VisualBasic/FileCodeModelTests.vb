' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public Class VisualBasicFileCodeModelTests
        Inherits AbstractFileCodeModelTests

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestEnumerationWithCountAndItem()
            Dim code =
<Code>
Namespace N
End Namespace
Class C
End Class
Interface I
End Interface
Structure S
End Structure
Enum E
    Foo
End Enum
Delegate Sub D()
</Code>
            Using workspaceAndFileCodeModel = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElements = workspaceAndFileCodeModel.FileCodeModel.CodeElements
                Dim count = codeElements.Count
                Assert.Equal(6, count)

                Dim expectedKinds = {EnvDTE.vsCMElement.vsCMElementNamespace,
                                     EnvDTE.vsCMElement.vsCMElementClass,
                                     EnvDTE.vsCMElement.vsCMElementInterface,
                                     EnvDTE.vsCMElement.vsCMElementStruct,
                                     EnvDTE.vsCMElement.vsCMElementEnum,
                                     EnvDTE.vsCMElement.vsCMElementDelegate}

                Dim expectedNames = {"N", "C", "I", "S", "E", "D"}

                For i = 0 To count - 1
                    Dim element = codeElements.Item(i + 1)
                    Assert.Equal(expectedKinds(i), element.Kind)
                    Assert.Equal(expectedNames(i), element.Name)
                Next

                Dim j As Integer
                For Each element As EnvDTE.CodeElement In codeElements
                    Assert.Equal(expectedKinds(j), element.Kind)
                    Assert.Equal(expectedNames(j), element.Name)
                    j += 1
                Next
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AssemblyLevelAttribute()
            Dim code =
<Code>
&lt;Assembly: Foo(0, True, S:="x")&gt;

Class FooAttribute
    Inherits System.Attribute

    Public Sub New(i As Integer, b As Boolean)
    End Sub

    Public Property S() As String
        Get
            Return String.Empty
        End Get
        Set(ByVal value As String)
        End Set
    End Property

End Class
</Code>

            Using workspaceAndFileCodeModel = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElements = workspaceAndFileCodeModel.FileCodeModel.CodeElements
                Dim count = codeElements.Count
                Assert.Equal(2, count)

                Dim codeAttribute = TryCast(codeElements.Item(1), EnvDTE80.CodeAttribute2)
                Assert.NotNull(codeAttribute)

                Assert.Same(workspaceAndFileCodeModel.FileCodeModel, codeAttribute.Parent)
                Assert.Equal("Foo", codeAttribute.Name)
                Assert.Equal("FooAttribute", codeAttribute.FullName)
                Assert.Equal("Assembly", codeAttribute.Target)
                Assert.Equal("0, True, S:=""x""", codeAttribute.Value)

                Dim arguments = codeAttribute.Arguments
                Assert.Equal(3, arguments.Count)

                Dim arg1 = TryCast(arguments.Item(1), EnvDTE80.CodeAttributeArgument)
                Assert.NotNull(arg1)
                Assert.Equal("", arg1.Name)
                Assert.Equal("0", arg1.Value)

                Dim arg2 = TryCast(arguments.Item(2), EnvDTE80.CodeAttributeArgument)
                Assert.NotNull(arg2)
                Assert.Equal("", arg2.Name)
                Assert.Equal("True", arg2.Value)

                Dim arg3 = TryCast(arguments.Item(3), EnvDTE80.CodeAttributeArgument)
                Assert.NotNull(arg3)
                Assert.Equal("S", arg3.Name)
                Assert.Equal("""x""", arg3.Value)
            End Using
        End Sub

        <WorkItem(1111417)>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CodeElementFullName()
            Dim code =
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                        <CompilationOptions RootNamespace="BarBaz"/>
                        <Document FilePath="Test1.vb">
Namespace Outer
    Public Class Class1
        Public Property Prop1 As Integer
        Public var1 As Class3
        Public Event event1()
        Public Function func1() As Integer
            Return 1
        End Function

        Public Class Class3
        End Class
    End Class
End Namespace</Document>
                    </Project>
                </Workspace>
            Using workspaceAndFileCodeModel = CreateCodeModelTestState(code)
                Dim codeElements = workspaceAndFileCodeModel.FileCodeModel.CodeElements

                Dim namespaceElement = TryCast(codeElements.Item(1), EnvDTE.CodeNamespace)
                Assert.NotNull(namespaceElement)
                Assert.Same(workspaceAndFileCodeModel.FileCodeModel, namespaceElement.Parent)
                Assert.Equal("Outer", namespaceElement.Name)
                Assert.Equal("BarBaz.Outer", namespaceElement.FullName)

                Dim codeClass = TryCast(namespaceElement.Members.Item(1), EnvDTE.CodeClass)
                Assert.NotNull(codeClass)
                Assert.Equal("Class1", codeClass.Name)
                Assert.Equal("BarBaz.Outer.Class1", codeClass.FullName)

                Dim classMembers = codeClass.Members

                Dim prop = TryCast(classMembers.Item(1), EnvDTE.CodeProperty)
                Assert.NotNull(prop)
                Assert.Equal("Prop1", prop.Name)
                Assert.Equal("BarBaz.Outer.Class1.Prop1", prop.FullName)

                Dim variable = TryCast(classMembers.Item(2), EnvDTE.CodeVariable)
                Assert.NotNull(variable)
                Assert.Equal("var1", variable.Name)
                Assert.Equal("BarBaz.Outer.Class1.var1", variable.FullName)
                Assert.Equal("BarBaz.Outer.Class1.Class3", variable.Type.AsFullName)

                Dim event1 = TryCast(classMembers.Item(3), EnvDTE80.CodeEvent)
                Assert.NotNull(event1)
                Assert.Equal("event1", event1.Name)
                Assert.Equal("BarBaz.Outer.Class1.event1", event1.FullName)

                Dim func1 = TryCast(classMembers.Item(4), EnvDTE.CodeFunction)
                Assert.NotNull(func1)
                Assert.Equal("func1", func1.Name)
                Assert.Equal("BarBaz.Outer.Class1.func1", func1.FullName)
            End Using
        End Sub

#Region "AddAttribute tests"

        Private Sub TestAddAttributeWithSimplification(
            code As XElement, expectedCode As XElement, data As AttributeData, expectedUnsimplifiedName As String, expectedSimplifiedName As String)
            TestAddAttributeWithSimplification(code, expectedCode,
                Sub(fileCodeModel, batch)
                    Dim newAttribute = fileCodeModel.AddAttribute(data.Name, data.Value, data.Position)
                    Assert.NotNull(newAttribute)
                    Assert.Equal(If(batch, expectedUnsimplifiedName, expectedSimplifiedName), newAttribute.Name)
                End Sub)
        End Sub

        Protected Sub TestAddAttributeWithSimplification(code As XElement, expectedCode As XElement, testOperation As Action(Of EnvDTE.FileCodeModel, Boolean))
            TestAddAttributeWithBatchMode(code, expectedCode, testOperation, False)
            TestAddAttributeWithBatchMode(code, expectedCode, testOperation, True)
        End Sub

        Private Sub TestAddAttributeWithBatchMode(code As XElement, expectedCode As XElement, testOperation As Action(Of EnvDTE.FileCodeModel, Boolean), batch As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                If batch Then
                    fileCodeModel.BeginBatch()
                End If

                testOperation(fileCodeModel, batch)

                If batch Then
                    fileCodeModel.EndBatch()
                End If

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()
                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
&lt;Assembly: CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestAddAttributeWithSimplification(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True"}, "System.CLSCompliant", "CLSCompliant")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
&lt;Assembly: CLSCompliant(True)&gt;
Class C
End Class
</Code>
            TestAddAttributeWithSimplification(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = "C"}, "System.CLSCompliant", "CLSCompliant")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute3()
            Dim code =
<Code>
$$&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
</Code>

            Dim expected =
<Code>
&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
&lt;Assembly: CLSCompliant(True)&gt;

</Code>

            TestAddAttributeWithSimplification(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = -1}, "System.CLSCompliant", "CLSCompliant")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute4()
            Dim code =
<Code>
$$&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;

Class C
End Class
</Code>

            Dim expected =
<Code>
&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
&lt;Assembly: CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestAddAttributeWithSimplification(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = -1}, "System.CLSCompliant", "CLSCompliant")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute5()
            Dim code =
<Code>
$$&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
&lt;Assembly: System.Reflection.AssemblyCopyright("2012")&gt;

Class C
End Class
</Code>

            Dim expected =
<Code>
&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
&lt;Assembly: System.Reflection.AssemblyCopyright("2012")&gt;
&lt;Assembly: CLSCompliant(True)&gt;
Class C
End Class</Code>

            TestAddAttributeWithSimplification(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = -1}, "System.CLSCompliant", "CLSCompliant")
        End Sub

#End Region

#Region "AddClass tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Public Class B
End Class

Class C
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass2()
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
Public Class B
End Class

Class C : End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass3()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class

Public Class B
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass4()
            Dim code =
<Code>
Class $$C : End Class
</Code>

            Dim expected =
<Code>
Class C : End Class

Public Class B
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass5()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class

Public Class B
    Inherits C
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = {"C"}})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass6()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class

Public Class B
    Inherits C
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = "C"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass7()
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            Dim expected =
<Code>
Interface I
End Interface

Public Class C
    Inherits I
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = {"I"}})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass8()
            Dim code =
<Code>
Interface $$I
End Interface
</Code>

            Dim expected =
<Code>
Interface I
End Interface

Public Class C
    Inherits I
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "I"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass9()
            Dim code =
<Code>
Class B : End Class
Interface $$I : End Interface
</Code>

            Dim expected =
<Code>
Class B : End Class
Interface I : End Interface

Public Class C
    Inherits B
    Implements I
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "B", .ImplementedInterfaces = "I"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass10()
            Dim code =
<Code>
Class B : End Class
Interface $$IFoo : End Interface
Interface IBar : End Interface
</Code>

            Dim expected =
<Code>
Class B : End Class
Interface IFoo : End Interface
Interface IBar : End Interface

Public Class C
    Inherits B
    Implements IFoo, IBar
End Class
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "IBar", .Bases = "B", .ImplementedInterfaces = {"IFoo", "IBar"}})
        End Sub

#End Region

#Region "AddImport tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System
Class C
End Class
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports S = System
Class C
End Class
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Alias = "S"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport3()
            Dim code =
<Code>
Imports System.Collections.Generic

Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System
Imports System.Collections.Generic

Class C
End Class
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport4()
            Dim code =
<Code>
Imports System.Collections.Generic

Class $$C
End Class
</Code>

            Dim expected =
<Code>
Imports System.Collections.Generic
Imports System

Class C
End Class
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Position = -1})
        End Sub

#End Region

#Region "AddNamespace tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Namespace N
End Namespace

Class C
End Class
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace2()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Namespace N
End Namespace

Class C
End Class
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace3()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
Class C
End Class

Namespace N
End Namespace
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = "C"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace4()
            Dim code =
<Code>$$</Code>

            Dim expected =
<Code>
Namespace N
End Namespace
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace5()
            Dim code =
<Code>
$$Imports System
</Code>

            Dim expected =
<Code>
Imports System

Namespace N
End Namespace
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace6()
            Dim code =
<Code>
$$Imports System
</Code>

            Dim expected =
<Code>
Imports System

Namespace N
End Namespace
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Sub
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace7()
            Dim code =
<Code>
$$Imports System
</Code>

            Dim expected =
<Code>
Imports System

Namespace N
End Namespace
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = Type.Missing})
        End Sub

#End Region

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClass()
            Dim code =
<Code>
Class C
End Class
</Code>

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElements = state.FileCodeModel.CodeElements

                Assert.Equal(1, codeElements.Count)

                Dim codeClass = TryCast(codeElements.Item(1), EnvDTE.CodeClass)
                Assert.NotNull(codeClass)

                Assert.Equal("C", codeClass.Name)
                Assert.Equal(1, codeClass.StartPoint.Line)
                Assert.Equal(1, codeClass.StartPoint.LineCharOffset)
                Assert.Equal(2, codeClass.EndPoint.Line)
                Assert.Equal(10, codeClass.EndPoint.LineCharOffset)
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClassWithTopLevelJunk()
            Dim code =
<Code>
Class C
End Class
A
</Code>

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElements = state.FileCodeModel.CodeElements

                Assert.Equal(1, codeElements.Count)

                Dim codeClass = TryCast(codeElements.Item(1), EnvDTE.CodeClass)
                Assert.NotNull(codeClass)

                Assert.Equal("C", codeClass.Name)
                Assert.Equal(1, codeClass.StartPoint.Line)
                Assert.Equal(1, codeClass.StartPoint.LineCharOffset)
                Assert.Equal(2, codeClass.EndPoint.Line)
                Assert.Equal(10, codeClass.EndPoint.LineCharOffset)
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClassNavigatePoints()
            Dim code =
<Code>
Class B
End Class

Class C
    Inherits B

End Class
</Code>

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElements = state.FileCodeModel.CodeElements

                Assert.Equal(2, codeElements.Count)

                Dim codeClassB = TryCast(codeElements.Item(1), EnvDTE.CodeClass)
                Assert.NotNull(codeClassB)
                Assert.Equal("B", codeClassB.Name)

                Dim startPointB = codeClassB.GetStartPoint(EnvDTE.vsCMPart.vsCMPartNavigate)
                Assert.Equal(2, startPointB.Line)
                Assert.Equal(1, startPointB.LineCharOffset)

                Dim codeClassC = TryCast(codeElements.Item(2), EnvDTE.CodeClass)
                Assert.NotNull(codeClassC)
                Assert.Equal("C", codeClassC.Name)

                Dim startPointC = codeClassC.GetStartPoint(EnvDTE.vsCMPart.vsCMPartNavigate)
                Assert.Equal(6, startPointC.Line)
                Assert.Equal(5, startPointC.LineCharOffset)
            End Using
        End Sub

        <WorkItem(579801)>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOptionStatement()
            Dim code =
<Code>
Option Explicit On
Class C

End Class
</Code>

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElements = state.FileCodeModel.CodeElements

                Assert.Equal(2, codeElements.Count)

                Dim optionStatement = codeElements.Item(1)
                Assert.NotNull(optionStatement)
                Assert.Equal(EnvDTE.vsCMElement.vsCMElementOptionStmt, optionStatement.Kind)

                Dim codeClassC = TryCast(codeElements.Item(2), EnvDTE.CodeClass)
                Assert.NotNull(codeClassC)
                Assert.Equal("C", codeClassC.Name)
            End Using
        End Sub

#Region "Remove tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Remove1()
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
</Code>

            TestRemoveChild(code, expected, "C")
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Remove2()
            Dim code =
<Code>
''' &lt;summary&gt;
'''
''' &lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
</Code>

            TestRemoveChild(code, expected, "C")
        End Sub

#End Region

        <ConditionalFact(GetType(x86))>
        Public Sub OutsideEditsFormattedAfterEndBatch()
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(<File>Class C : End Class</File>))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                fileCodeModel.BeginBatch()

                ' Make an outside edit not through the CodeModel APIs
                Dim buffer = state.Workspace.Documents.Single().TextBuffer
                buffer.Replace(New Text.Span(0, 1), "c")

                fileCodeModel.EndBatch()

                Assert.Contains("Class C", buffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using

        End Sub

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CreateUnknownElementForDeclarationFunctionAndSub()
            Dim oldCode =
<Code>
Public Class Class1 
Public Declare Sub f1 Lib "MyLib.dll" () 
Public Declare Function f2 Lib "MyLib.dll" () As Integer 
End Class
</Code>
            Dim changedCodeRemoveFunction =
<Code>
Public Class Class1 
Public Declare Sub f1 Lib "MyLib.dll" () 
End Class
</Code>
            Dim changedCodeSubFunction =
<Code>
Public Class Class1 
Public Declare Function f2 Lib "MyLib.dll" () As Integer 
End Class
</Code>

            Dim changedDefinition =
<Workspace>
    <Project Language=<%= LanguageName %> CommonReferences="true">
        <Document FilePath="File1.vb"><%= changedCodeRemoveFunction.Value %></Document>
        <Document FilePath="File2.vb"><%= changedCodeSubFunction.Value %></Document>
    </Project>
</Workspace>

            Using originalWorkspaceAndFileCodeModel = CreateCodeModelTestState(GetWorkspaceDefinition(oldCode))
                Using changedworkspace = TestWorkspaceFactory.CreateWorkspace(changedDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)

                    Dim originalDocument = originalWorkspaceAndFileCodeModel.Workspace.CurrentSolution.GetDocument(originalWorkspaceAndFileCodeModel.Workspace.Documents(0).Id)
                    Dim originalTree = originalDocument.GetSyntaxTreeAsync().Result

                    ' Assert Declaration Function Removal
                    Dim changeDocument = changedworkspace.CurrentSolution.GetDocument(changedworkspace.Documents.First(Function(d) d.Name.Equals("File1.vb")).Id)
                    Dim changeTree = changeDocument.GetSyntaxTreeAsync().Result

                    Dim codeModelEvent = originalWorkspaceAndFileCodeModel.CodeModelService.CollectCodeModelEvents(originalTree, changeTree)
                    Dim fileCodeModel = originalWorkspaceAndFileCodeModel.FileCodeModelObject

                    Dim element As EnvDTE.CodeElement = Nothing
                    Dim parentElement As Object = Nothing
                    fileCodeModel.GetElementsForCodeModelEvent(codeModelEvent.First(), element, parentElement)
                    Assert.NotNull(element)
                    Assert.NotNull(parentElement)

                    Dim unknownCodeFunction = TryCast(element, EnvDTE.CodeFunction)
                    Assert.Equal(unknownCodeFunction.Name, "f2")

                    ' Assert Declaration Sub Removal
                    changeDocument = changedworkspace.CurrentSolution.GetDocument(changedworkspace.Documents.First(Function(d) d.Name.Equals("File2.vb")).Id)
                    changeTree = changeDocument.GetSyntaxTreeAsync().Result

                    codeModelEvent = originalWorkspaceAndFileCodeModel.CodeModelService.CollectCodeModelEvents(originalTree, changeTree)

                    element = Nothing
                    parentElement = Nothing
                    fileCodeModel.GetElementsForCodeModelEvent(codeModelEvent.First(), element, parentElement)
                    Assert.NotNull(element)
                    Assert.NotNull(parentElement)

                    unknownCodeFunction = TryCast(element, EnvDTE.CodeFunction)
                    Assert.Equal(unknownCodeFunction.Name, "f1")

                End Using
            End Using
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class
End Namespace
