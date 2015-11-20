' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Public Class VisualBasicFileCodeModelTests
        Inherits AbstractFileCodeModelTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestEnumerationWithCountAndItem() As Task
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
            Using workspaceAndFileCodeModel = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAssemblyLevelAttribute() As Task
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

            Using workspaceAndFileCodeModel = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
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
        End Function

        <WorkItem(1111417)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeElementFullName() As Task
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
            Using workspaceAndFileCodeModel = Await CreateCodeModelTestStateAsync(code)
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
        End Function

#Region "AddAttribute tests"

        Private Function TestAddAttributeWithSimplificationAsync(
            code As XElement, expectedCode As XElement, data As AttributeData, expectedUnsimplifiedName As String, expectedSimplifiedName As String) As Task
            Return TestAddAttributeWithSimplificationAsync(code, expectedCode,
                Sub(fileCodeModel, batch)
                    Dim newAttribute = fileCodeModel.AddAttribute(data.Name, data.Value, data.Position)
                    Assert.NotNull(newAttribute)
                    Assert.Equal(If(batch, expectedUnsimplifiedName, expectedSimplifiedName), newAttribute.Name)
                End Sub)
        End Function

        Protected Async Function TestAddAttributeWithSimplificationAsync(code As XElement, expectedCode As XElement, testOperation As Action(Of EnvDTE.FileCodeModel, Boolean)) As Task
            Await TestAddAttributeWithBatchModeAsync(code, expectedCode, testOperation, False)
            Await TestAddAttributeWithBatchModeAsync(code, expectedCode, testOperation, True)
        End Function

        Private Async Function TestAddAttributeWithBatchModeAsync(code As XElement, expectedCode As XElement, testOperation As Action(Of EnvDTE.FileCodeModel, Boolean), batch As Boolean) As Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
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

            Await TestAddAttributeWithSimplificationAsync(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True"}, "System.CLSCompliant", "CLSCompliant")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
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
            Await TestAddAttributeWithSimplificationAsync(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = "C"}, "System.CLSCompliant", "CLSCompliant")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute3() As Task
            Dim code =
<Code>
$$&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
</Code>

            Dim expected =
<Code>
&lt;Assembly: System.Reflection.AssemblyCompany("Microsoft")&gt;
&lt;Assembly: CLSCompliant(True)&gt;

</Code>

            Await TestAddAttributeWithSimplificationAsync(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = -1}, "System.CLSCompliant", "CLSCompliant")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute4() As Task
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

            Await TestAddAttributeWithSimplificationAsync(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = -1}, "System.CLSCompliant", "CLSCompliant")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute5() As Task
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

            Await TestAddAttributeWithSimplificationAsync(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True", .Position = -1}, "System.CLSCompliant", "CLSCompliant")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute6() As Task
            Dim code =
<Code>
''' &lt;summary&gt;&lt;/summary&gt;
Class $$C
End Class
</Code>

            Dim expected =
<Code>
&lt;Assembly: CLSCompliant(True)&gt;

''' &lt;summary&gt;&lt;/summary&gt;
Class C
End Class
</Code>

            Await TestAddAttributeWithSimplificationAsync(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "True"}, "System.CLSCompliant", "CLSCompliant")
        End Function

#End Region

#Region "AddClass tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass1() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass2() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass3() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass4() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass5() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = {"C"}})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass6() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = "C"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass7() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = {"I"}})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass8() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "I"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass9() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "B", .ImplementedInterfaces = "I"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass10() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "IBar", .Bases = "B", .ImplementedInterfaces = {"IFoo", "IBar"}})
        End Function

#End Region

#Region "AddImport tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport1() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport2() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Alias = "S"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport3() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport4() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Position = -1})
        End Function

#End Region

#Region "AddNamespace tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace1() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace2() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace3() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = "C"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace4() As Task
            Dim code =
<Code>$$</Code>

            Dim expected =
<Code>
Namespace N
End Namespace
</Code>

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace5() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace6() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Function
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace7() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = Type.Missing})
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClass() As Task
            Dim code =
<Code>
Class C
End Class
</Code>

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassWithTopLevelJunk() As Task
            Dim code =
<Code>
Class C
End Class
A
</Code>

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
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
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestClassNavigatePoints() As Task
            Dim code =
<Code>
Class B
End Class

Class C
    Inherits B

End Class
</Code>

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
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
        End Function

        <WorkItem(579801)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOptionStatement() As Task
            Dim code =
<Code>
Option Explicit On
Class C

End Class
</Code>

            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim codeElements = state.FileCodeModel.CodeElements

                Assert.Equal(2, codeElements.Count)

                Dim optionStatement = codeElements.Item(1)
                Assert.NotNull(optionStatement)
                Assert.Equal(EnvDTE.vsCMElement.vsCMElementOptionStmt, optionStatement.Kind)

                Dim codeClassC = TryCast(codeElements.Item(2), EnvDTE.CodeClass)
                Assert.NotNull(codeClassC)
                Assert.Equal("C", codeClassC.Name)
            End Using
        End Function

#Region "Remove tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove1() As Task
            Dim code =
<Code>
Class $$C
End Class
</Code>

            Dim expected =
<Code>
</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove2() As Task
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

            Await TestRemoveChild(code, expected, "C")
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86))>
        Public Async Function TestOutsideEditsFormattedAfterEndBatch() As Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(<File>Class C : End Class</File>))
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                fileCodeModel.BeginBatch()

                ' Make an outside edit not through the CodeModel APIs
                Dim buffer = state.Workspace.Documents.Single().TextBuffer
                buffer.Replace(New Text.Span(0, 1), "c")

                fileCodeModel.EndBatch()

                Assert.Contains("Class C", buffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using

        End Function

        <WorkItem(925569)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function ChangeClassNameAndGetNameOfChildFunction() As Task
            Dim code =
    <Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Await TestOperation(code,
                Sub(fileCodeModel)
                    Dim codeClass = TryCast(fileCodeModel.CodeElements.Item(1), EnvDTE.CodeClass)
                    Assert.NotNull(codeClass)
                    Assert.Equal("C", codeClass.Name)

                    Dim codeFunction = TryCast(codeClass.Members.Item(1), EnvDTE.CodeFunction)
                    Assert.NotNull(codeFunction)
                    Assert.Equal("M", codeFunction.Name)

                    codeClass.Name = "NewClassName"
                    Assert.Equal("NewClassName", codeClass.Name)
                    Assert.Equal("M", codeFunction.Name)
                End Sub)
        End Function

        <WorkItem(2355, "https://github.com/dotnet/roslyn/issues/2355")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function CreateUnknownElementForDeclarationFunctionAndSub() As Task
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

            Using originalWorkspaceAndFileCodeModel = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(oldCode))
                Using changedworkspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(changedDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)

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
        End Function

        <WorkItem(858153)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeElements_InheritsStatements() As Task
            Dim code =
    <code>
Class A
End Class

Class C
    Inherits A
End Class
</code>

            Await TestOperation(code,
                    Sub(fileCodeModel)
                        Dim classC = TryCast(fileCodeModel.CodeElements.Item(2), EnvDTE.CodeClass)
                        Assert.NotNull(classC)
                        Assert.Equal("C", classC.Name)

                        Dim inheritsA = TryCast(classC.Children.Item(1), EnvDTE80.CodeElement2)
                        Assert.NotNull(inheritsA)

                        Dim parent = TryCast(inheritsA.Collection.Parent, EnvDTE.CodeClass)
                        Assert.NotNull(parent)
                        Assert.Equal("C", parent.Name)

                        ' This assert is very important!
                        '
                        ' We are testing that we don't regress a bug where the VB Inherits statement creates its
                        ' parent incorrectly such that *existing* Code Model objects for its parent ("C") get a different
                        ' NodeKey that makes the existing objects invalid. If the bug regresses, the line below will
                        ' fail with an ArguementException when trying to use classC's NodeKey to lookup its node.
                        ' (Essentially, its NodeKey will be {C,2} rather than {C,1}).
                        Assert.Equal("C", classC.Name)

                        ' Sanity: ensure that the NodeKeys are correct
                        Dim member1 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(parent)
                        Dim member2 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(classC)

                        Assert.Equal("C", member1.NodeKey.Name)
                        Assert.Equal(1, member1.NodeKey.Ordinal)
                        Assert.Equal("C", member2.NodeKey.Name)
                        Assert.Equal(1, member2.NodeKey.Ordinal)
                    End Sub)
        End Function

        <WorkItem(858153)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeElements_ImplementsStatements() As Task
            Dim code =
    <code>
Interface I
End Interface

Class C
    Implements I
End Class
</code>

            Await TestOperation(code,
                    Sub(fileCodeModel)
                        Dim classC = TryCast(fileCodeModel.CodeElements.Item(2), EnvDTE.CodeClass)
                        Assert.NotNull(classC)
                        Assert.Equal("C", classC.Name)

                        Dim implementsI = TryCast(classC.Children.Item(1), EnvDTE80.CodeElement2)
                        Assert.NotNull(implementsI)

                        Dim parent = TryCast(implementsI.Collection.Parent, EnvDTE.CodeClass)
                        Assert.NotNull(parent)
                        Assert.Equal("C", parent.Name)

                        ' This assert is very important!
                        '
                        ' We are testing that we don't regress a bug where the VB Implements statement creates its
                        ' parent incorrectly such that *existing* Code Model objects for its parent ("C") get a different
                        ' NodeKey that makes the existing objects invalid. If the bug regresses, the line below will
                        ' fail with an ArguementException when trying to use classC's NodeKey to lookup its node.
                        ' (Essentially, its NodeKey will be {C,2} rather than {C,1}).
                        Assert.Equal("C", classC.Name)

                        ' Sanity: ensure that the NodeKeys are correct
                        Dim member1 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(parent)
                        Dim member2 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(classC)

                        Assert.Equal("C", member1.NodeKey.Name)
                        Assert.Equal(1, member1.NodeKey.Ordinal)
                        Assert.Equal("C", member2.NodeKey.Name)
                        Assert.Equal(1, member2.NodeKey.Ordinal)
                    End Sub)
        End Function

        <WorkItem(858153)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeElements_PropertyAccessor() As Task
            Dim code =
    <code>
Class C
    ReadOnly Property P As Integer
        Get
        End Get
    End Property
End Class
</code>

            Await TestOperation(code,
                    Sub(fileCodeModel)
                        Dim classC = TryCast(fileCodeModel.CodeElements.Item(1), EnvDTE.CodeClass)
                        Assert.NotNull(classC)
                        Assert.Equal("C", classC.Name)

                        Dim propertyP = TryCast(classC.Members.Item(1), EnvDTE.CodeProperty)
                        Assert.NotNull(propertyP)
                        Assert.Equal("P", propertyP.Name)

                        Dim getter = propertyP.Getter
                        Assert.NotNull(getter)

                        Dim searchedGetter = fileCodeModel.CodeElementFromPoint(getter.StartPoint, EnvDTE.vsCMElement.vsCMElementFunction)

                        Dim parent = TryCast(getter.Collection.Parent, EnvDTE.CodeProperty)
                        Assert.NotNull(parent)
                        Assert.Equal("P", parent.Name)

                        ' This assert is very important!
                        '
                        ' We are testing that we don't regress a bug where a property accessor creates its
                        ' parent incorrectly such that *existing* Code Model objects for its parent ("P") get a different
                        ' NodeKey that makes the existing objects invalid. If the bug regresses, the line below will
                        ' fail with an ArguementException when trying to use propertyP's NodeKey to lookup its node.
                        ' (Essentially, its NodeKey will be {C.P As Integer,2} rather than {C.P As Integer,1}).
                        Assert.Equal("P", propertyP.Name)

                        ' Sanity: ensure that the NodeKeys are correct
                        Dim member1 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(parent)
                        Dim member2 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(propertyP)

                        Assert.Equal("C.P As Integer", member1.NodeKey.Name)
                        Assert.Equal(1, member1.NodeKey.Ordinal)
                        Assert.Equal("C.P As Integer", member2.NodeKey.Name)
                        Assert.Equal(1, member2.NodeKey.Ordinal)
                    End Sub)
        End Function

        <WorkItem(858153)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeElements_EventAccessor() As Task
            Dim code =
    <code>
Class C
    Custom Event E As System.EventHandler
        AddHandler(value As System.EventHandler)

        End AddHandler
        RemoveHandler(value As System.EventHandler)

        End RemoveHandler
        RaiseEvent(sender As Object, e As System.EventArgs)

        End RaiseEvent
    End Event
End Class
</code>

            Await TestOperation(code,
                Sub(fileCodeModel)
                    Dim classC = TryCast(fileCodeModel.CodeElements.Item(1), EnvDTE.CodeClass)
                    Assert.NotNull(classC)
                    Assert.Equal("C", classC.Name)

                    Dim eventE = TryCast(classC.Members.Item(1), EnvDTE80.CodeEvent)
                    Assert.NotNull(eventE)
                    Assert.Equal("E", eventE.Name)

                    Dim adder = eventE.Adder
                    Assert.NotNull(adder)

                    Dim searchedAdder = fileCodeModel.CodeElementFromPoint(adder.StartPoint, EnvDTE.vsCMElement.vsCMElementFunction)

                    Dim parent = TryCast(adder.Collection.Parent, EnvDTE80.CodeEvent)
                    Assert.NotNull(parent)
                    Assert.Equal("E", parent.Name)

                    ' This assert is very important!
                    '
                    ' We are testing that we don't regress a bug where an event accessor creates its
                    ' parent incorrectly such that *existing* Code Model objects for its parent ("E") get a different
                    ' NodeKey that makes the existing objects invalid. If the bug regresses, the line below will
                    ' fail with an ArguementException when trying to use propertyP's NodeKey to lookup its node.
                    ' (Essentially, its NodeKey will be {C.E As System.EventHandler,2} rather than {C.E As System.EventHandler,1}).
                    Assert.Equal("E", eventE.Name)

                    ' Sanity: ensure that the NodeKeys are correct
                    Dim member1 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(parent)
                    Dim member2 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(eventE)

                    Assert.Equal("C.E As System.EventHandler", member1.NodeKey.Name)
                    Assert.Equal(1, member1.NodeKey.Ordinal)
                    Assert.Equal("C.E As System.EventHandler", member2.NodeKey.Name)
                    Assert.Equal(1, member2.NodeKey.Ordinal)
                End Sub)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class
End Namespace
