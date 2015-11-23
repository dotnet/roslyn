' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CSharpFileCodeModelTests
        Inherits AbstractFileCodeModelTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestEnumerationWithCountAndItem()
            Dim code =
<Code>
namespace N { }
class C { }
interface I { }
struct S { }
enum E { }
delegate void D();
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AssemblyLevelAttribute()
            Dim code =
<Code>
[assembly: Foo(0, true, S = "x")]

class FooAttribute : System.Attribute
{
    public FooAttribute(int i, bool b) { }

    public string S { get { return string.Empty; } set { } }
}
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
                Assert.Equal("assembly", codeAttribute.Target)
                Assert.Equal("0, true, S = ""x""", codeAttribute.Value)

                Dim arguments = codeAttribute.Arguments
                Assert.Equal(3, arguments.Count)

                Dim arg1 = TryCast(arguments.Item(1), EnvDTE80.CodeAttributeArgument)
                Assert.NotNull(arg1)
                Assert.Equal("", arg1.Name)
                Assert.Equal("0", arg1.Value)

                Dim arg2 = TryCast(arguments.Item(2), EnvDTE80.CodeAttributeArgument)
                Assert.NotNull(arg2)
                Assert.Equal("", arg2.Name)
                Assert.Equal("true", arg2.Value)

                Dim arg3 = TryCast(arguments.Item(3), EnvDTE80.CodeAttributeArgument)
                Assert.NotNull(arg3)
                Assert.Equal("S", arg3.Name)
                Assert.Equal("""x""", arg3.Value)
            End Using
        End Sub

#Region "AddAttribute tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
[assembly: System.CLSCompliant(true)]

class C
{
}
</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
[assembly: System.CLSCompliant(true)]

class C
{
}
</Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = "C"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute3()
            Dim code =
<Code>
$$[assembly: System.Reflection.AssemblyCompany("Microsoft")]
</Code>

            Dim expected =
<Code>
[assembly: System.Reflection.AssemblyCompany("Microsoft")]
[assembly: System.CLSCompliant(true)]

</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute4()
            Dim code =
<Code>
$$[assembly: System.Reflection.AssemblyCompany("Microsoft")]

class C { }
</Code>

            Dim expected =
<Code>
[assembly: System.Reflection.AssemblyCompany("Microsoft")]
[assembly: System.CLSCompliant(true)]

class C { }
</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute5()
            Dim code =
<Code>
$$[assembly: System.Reflection.AssemblyCompany("Microsoft")]
[assembly: System.Reflection.AssemblyCopyright("2012")]

class C { }
</Code>

            Dim expected =
<Code>
[assembly: System.Reflection.AssemblyCompany("Microsoft")]
[assembly: System.Reflection.AssemblyCopyright("2012")]
[assembly: System.CLSCompliant(true)]

class C { }
</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute6()
            Dim code =
<Code>
/// &lt;summary&gt;&lt;/summary&gt;
class $$C { }
</Code>

            Dim expected =
<Code>
[assembly: System.CLSCompliant(true)]
/// &lt;summary&gt;&lt;/summary&gt;
class C { }
</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true"})
        End Sub

#End Region

#Region "AddClass tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class B
{
}

class C
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass2()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class B
{
}

class C { }
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass3()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

class B
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass4()
            Dim code =
<Code>
class $$C { }
</Code>

            Dim expected =
<Code>
class C { }

class B
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass5()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

class B : C
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = {"C"}})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass6()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

class B : C
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = "C"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass7()
            Dim code =
<Code>
interface $$I
{
}
</Code>

            Dim expected =
<Code>
interface I
{
}

class C : I
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = {"I"}})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass8()
            Dim code =
<Code>
interface $$I
{
}
</Code>

            Dim expected =
<Code>
interface I
{
}

class C : I
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "I"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass9()
            Dim code =
<Code>
class B { }
interface $$I { }
</Code>

            Dim expected =
<Code>
class B { }
interface I { }

class C : B, I
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "B", .ImplementedInterfaces = "I"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass10()
            Dim code =
<Code>
class B { }
interface $$IFoo { }
interface IBar { }
</Code>

            Dim expected =
<Code>
class B { }
interface IFoo { }
interface IBar { }

class C : B, IFoo, IBar
{
}
</Code>

            TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "IBar", .Bases = "B", .ImplementedInterfaces = {"IFoo", "IBar"}})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddClass_Stress()
            Dim code =
<Code>
class B { }
interface $$IFoo { }
interface IBar { }
</Code>

            TestOperation(code,
                Sub(fileCodeModel)
                    For i = 1 To 100
                        Dim name = $"C{i}"
                        Dim newClass = fileCodeModel.AddClass(name, Position:=-1, Bases:="B", ImplementedInterfaces:={"IFoo", "IBar"})
                        Assert.NotNull(newClass)
                        Assert.Equal(name, newClass.Name)
                    Next
                End Sub)
        End Sub

#End Region

#Region "AddDelegate tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddDelegate1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
delegate void D();

class C
{
}
</Code>

            TestAddDelegate(code, expected, New DelegateData With {.Name = "D", .Type = "void"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddDelegate2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

delegate int D();
</Code>

            TestAddDelegate(code, expected, New DelegateData With {.Name = "D", .Type = "int", .Position = "C"})
        End Sub

#End Region

#Region "AddEnum tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddEnum1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
enum E
{
}

class C
{
}
</Code>

            TestAddEnum(code, expected, New EnumData With {.Name = "E"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddEnum2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

enum E
{
}
</Code>

            TestAddEnum(code, expected, New EnumData With {.Name = "E", .Position = "C"})
        End Sub

#End Region

#Region "AddImport tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
using System;

class C
{
}
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
using S = System;

class C
{
}
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Alias = "S"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport3()
            Dim code =
<Code>
using System.Collections.Generic;

class $$C
{
}
</Code>

            Dim expected =
<Code>
using System;
using System.Collections.Generic;

class C
{
}
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddImport4()
            Dim code =
<Code>
using System.Collections.Generic;

class $$C
{
}
</Code>

            Dim expected =
<Code>
using System.Collections.Generic;
using System;

class C
{
}
</Code>

            TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Position = -1})
        End Sub

#End Region

#Region "AddInterface tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddInterface1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
interface I
{
}

class C
{
}
</Code>

            TestAddInterface(code, expected, New InterfaceData With {.Name = "I"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddInterface2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

interface I
{
}
</Code>

            TestAddInterface(code, expected, New InterfaceData With {.Name = "I", .Position = "C"})
        End Sub

#End Region

#Region "AddNamespace tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
namespace N
{
}

class C
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
namespace N
{
}

class C
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace3()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

namespace N
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = "C"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace4()
            Dim code =
<Code>$$</Code>

            Dim expected =
<Code>
namespace N
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace5()
            Dim code =
<Code>
$$using System;
</Code>

            Dim expected =
<Code>
using System;

namespace N
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace6()
            Dim code =
<Code>
$$using System;
</Code>

            Dim expected =
<Code>
using System;

namespace N
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddNamespace7()
            Dim code =
<Code>
$$using System;
</Code>

            Dim expected =
<Code>
using System;

namespace N
{
}
</Code>

            TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = Type.Missing})
        End Sub

#End Region

#Region "AddStruct tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddStruct1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
struct S
{
}

class C
{
}
</Code>

            TestAddStruct(code, expected, New StructData With {.Name = "S"})
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddStruct2()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
class C
{
}

struct S
{
}
</Code>

            TestAddStruct(code, expected, New StructData With {.Name = "S", .Position = "C"})
        End Sub

#End Region

#Region "Remove tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Remove1()
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
</Code>

            TestRemoveChild(code, expected, "C")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Remove2()
            Dim code =
<Code>
/// &lt;summary&gt;
///
/// &lt;/summary&gt;
Class $$C
{
}
</Code>

            Dim expected =
<Code>
</Code>

            TestRemoveChild(code, expected, "C")
        End Sub

#End Region

        <WorkItem(921220)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClosedDocument()
            Dim code =
<Code>
class $$C
{
    void M() { }
}
</Code>
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeClass = state.GetCodeElementAtCursor(Of EnvDTE80.CodeClass2)
                Assert.Equal(1, codeClass.Members.OfType(Of EnvDTE80.CodeFunction2)().Count())
                Dim project = state.VisualStudioWorkspace.CurrentSolution.Projects.First()
                Dim documentId = project.DocumentIds.First()
                state.VisualStudioWorkspace.CloseDocument(documentId)
                Dim newSolution = state.VisualStudioWorkspace.CurrentSolution.RemoveDocument(documentId)
                state.VisualStudioWorkspace.TryApplyChanges(newSolution)
                ' throws COMException with HResult = E_FAIL
                Assert.Throws(Of System.Runtime.InteropServices.COMException)(
                    Sub()
                        Dim count = codeClass.Members.OfType(Of EnvDTE80.CodeFunction2)().Count()
                    End Sub)
            End Using
        End Sub

        <WorkItem(1980, "https://github.com/dotnet/roslyn/issues/1980")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CreateUnknownElementForConversionOperator()
            Dim oldCode =
<Code>
class D
{
    public static implicit operator D(double d)
    {
        return new D();
    }
}
</Code>
            Dim changedCode =
<Code>
class D
{
}
</Code>

            Dim changedDefinition =
<Workspace>
    <Project Language=<%= LanguageName %> CommonReferences="true">
        <Document><%= changedCode.Value %></Document>
    </Project>
</Workspace>

            Using originalWorkspaceAndFileCodeModel = CreateCodeModelTestState(GetWorkspaceDefinition(oldCode))
                Using changedworkspace = TestWorkspaceFactory.CreateWorkspace(changedDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)

                    Dim originalDocument = originalWorkspaceAndFileCodeModel.Workspace.CurrentSolution.GetDocument(originalWorkspaceAndFileCodeModel.Workspace.Documents(0).Id)
                    Dim originalTree = originalDocument.GetSyntaxTreeAsync().Result

                    Dim changeDocument = changedworkspace.CurrentSolution.GetDocument(changedworkspace.Documents(0).Id)
                    Dim changeTree = changeDocument.GetSyntaxTreeAsync().Result

                    Dim codeModelEvent = originalWorkspaceAndFileCodeModel.CodeModelService.CollectCodeModelEvents(originalTree, changeTree)
                    Dim fileCodeModel = originalWorkspaceAndFileCodeModel.FileCodeModelObject

                    Dim element As EnvDTE.CodeElement = Nothing
                    Dim parentElement As Object = Nothing
                    fileCodeModel.GetElementsForCodeModelEvent(codeModelEvent.First(), element, parentElement)
                    Assert.NotNull(element)
                    Assert.NotNull(parentElement)

                    Dim unknownCodeFunction = TryCast(element, EnvDTE.CodeFunction)
                    Assert.Equal(unknownCodeFunction.Name, "implicit operator D")
                End Using
            End Using
        End Sub

        <WorkItem(925569)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ChangeClassNameAndGetNameOfChildFunction()
            Dim code =
<Code>
class C
{
    void M() { }
}
</Code>

            TestOperation(code,
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
        End Sub

        <WorkItem(858153)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeElements_PropertyAccessor()
            Dim code =
<code>
class C
{
    int P
    {
        get { return 0; }
    }
}
</code>

            TestOperation(code,
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
                    ' (Essentially, its NodeKey will be {C.P,2} rather than {C.P,1}).
                    Assert.Equal("P", propertyP.Name)

                    ' Sanity: ensure that the NodeKeys are correct
                    Dim member1 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(parent)
                    Dim member2 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(propertyP)

                    Assert.Equal("C.P", member1.NodeKey.Name)
                    Assert.Equal(1, member1.NodeKey.Ordinal)
                    Assert.Equal("C.P", member2.NodeKey.Name)
                    Assert.Equal(1, member2.NodeKey.Ordinal)
                End Sub)
        End Sub

        <WorkItem(858153)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeElements_EventAccessor()
            Dim code =
<code>
class C
{
    event System.EventHandler E
    {
        add { }
        remove { }
    }
}
</code>

            TestOperation(code,
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
                    ' parent incorrectly such that *existing* Code Model objects for its parent ("P") get a different
                    ' NodeKey that makes the existing objects invalid. If the bug regresses, the line below will
                    ' fail with an ArguementException when trying to use propertyP's NodeKey to lookup its node.
                    ' (Essentially, its NodeKey will be {C.E,2} rather than {C.E,1}).
                    Assert.Equal("E", eventE.Name)

                    ' Sanity: ensure that the NodeKeys are correct
                    Dim member1 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(parent)
                    Dim member2 = ComAggregate.GetManagedObject(Of AbstractCodeMember)(eventE)

                    Assert.Equal("C.E", member1.NodeKey.Name)
                    Assert.Equal(1, member1.NodeKey.Ordinal)
                    Assert.Equal("C.E", member2.NodeKey.Name)
                    Assert.Equal(1, member2.NodeKey.Ordinal)
                End Sub)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
