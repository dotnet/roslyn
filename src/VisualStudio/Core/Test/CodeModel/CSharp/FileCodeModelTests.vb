' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CSharpFileCodeModelTests
        Inherits AbstractFileCodeModelTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAssemblyLevelAttribute()
            Dim code =
<Code>
[assembly: Goo(0, true, S = "x")]

class GooAttribute : System.Attribute
{
    public GooAttribute(int i, bool b) { }

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
                Assert.Equal("Goo", codeAttribute.Name)
                Assert.Equal("GooAttribute", codeAttribute.FullName)
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150349")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub NoChildrenForInvalidMembers()
            Dim code =
<Code>
void M() { }
int P { get { return 42; } }
event System.EventHandler E;
class C { }
</Code>

            TestChildren(code,
                IsElement("C"))
        End Sub

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
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

            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
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
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = "C"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute3() As Task
            Dim code =
<Code>
$$[assembly: System.Reflection.AssemblyCompany("Microsoft")]
</Code>

            Dim expected =
<Code>
[assembly: System.Reflection.AssemblyCompany("Microsoft")]
[assembly: System.CLSCompliant(true)]

</Code>

            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute4() As Task
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

            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute5() As Task
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

            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute6() As Task
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

            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true"})
        End Function

#End Region

#Region "AddClass tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass1() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass2() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass3() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass4() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass5() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = {"C"}})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass6() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "B", .Position = "C", .Bases = "C"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass7() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = {"I"}})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass8() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "I"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass9() As Task
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

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "I", .Bases = "B", .ImplementedInterfaces = "I"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddClass10() As Task
            Dim code =
<Code>
class B { }
interface $$IGoo { }
interface IBar { }
</Code>

            Dim expected =
<Code>
class B { }
interface IGoo { }
interface IBar { }

class C : B, IGoo, IBar
{
}
</Code>

            Await TestAddClass(code, expected, New ClassData With {.Name = "C", .Position = "IBar", .Bases = "B", .ImplementedInterfaces = {"IGoo", "IBar"}})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddClass_Stress()
            Dim code =
<Code>
class B { }
interface $$IGoo { }
interface IBar { }
</Code>

            TestOperation(code,
                Sub(fileCodeModel)
                    For i = 1 To 100
                        Dim name = $"C{i}"
                        Dim newClass = fileCodeModel.AddClass(name, Position:=-1, Bases:="B", ImplementedInterfaces:={"IGoo", "IBar"})
                        Assert.NotNull(newClass)
                        Assert.Equal(name, newClass.Name)
                    Next
                End Sub)
        End Sub

#End Region

#Region "AddDelegate tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddDelegate1() As Task
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

            Await TestAddDelegate(code, expected, New DelegateData With {.Name = "D", .Type = "void"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddDelegate2() As Task
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

            Await TestAddDelegate(code, expected, New DelegateData With {.Name = "D", .Type = "int", .Position = "C"})
        End Function

#End Region

#Region "AddEnum tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddEnum1() As Task
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

            Await TestAddEnum(code, expected, New EnumData With {.Name = "E"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddEnum2() As Task
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

            Await TestAddEnum(code, expected, New EnumData With {.Name = "E", .Position = "C"})
        End Function

#End Region

#Region "AddImport tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport1() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport2() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Alias = "S"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport3() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddImport4() As Task
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

            Await TestAddImport(code, expected, New ImportData With {.[Namespace] = "System", .Position = -1})
        End Function

#End Region

#Region "AddInterface tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddInterface1() As Task
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

            Await TestAddInterface(code, expected, New InterfaceData With {.Name = "I"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddInterface2() As Task
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

            Await TestAddInterface(code, expected, New InterfaceData With {.Name = "I", .Position = "C"})
        End Function

#End Region

#Region "AddNamespace tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace1() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace2() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace3() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = "C"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace4() As Task
            Dim code =
<Code>$$</Code>

            Dim expected =
<Code>
namespace N
{
}
</Code>

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace5() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace6() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = 0})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddNamespace7() As Task
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

            Await TestAddNamespace(code, expected, New NamespaceData With {.Name = "N", .Position = Type.Missing})
        End Function

#End Region

#Region "AddStruct tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddStruct1() As Task
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

            Await TestAddStruct(code, expected, New StructData With {.Name = "S"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddStruct2() As Task
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

            Await TestAddStruct(code, expected, New StructData With {.Name = "S", .Position = "C"})
        End Function

#End Region

#Region "Remove tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove1() As Task
            Dim code =
<Code>
class $$C
{
}
</Code>

            Dim expected =
<Code>
</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemove2() As Task
            Dim code =
<Code>
/// &lt;summary&gt;
///
/// &lt;/summary&gt;
class $$C
{
}
</Code>

            Dim expected =
<Code>
</Code>

            Await TestRemoveChild(code, expected, "C")
        End Function

#End Region

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/921220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClosedDocument()
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/1980")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCreateUnknownElementForConversionOperator() As Task
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
                Using changedWorkspace = EditorTestWorkspace.Create(changedDefinition, composition:=CodeModelTestHelpers.Composition)

                    Dim originalDocument = originalWorkspaceAndFileCodeModel.Workspace.CurrentSolution.GetDocument(originalWorkspaceAndFileCodeModel.Workspace.Documents(0).Id)
                    Dim originalTree = Await originalDocument.GetSyntaxTreeAsync()

                    Dim changeDocument = changedWorkspace.CurrentSolution.GetDocument(changedWorkspace.Documents(0).Id)
                    Dim changeTree = Await changeDocument.GetSyntaxTreeAsync()

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
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/925569")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestChangeClassNameAndGetNameOfChildFunction()
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858153")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858153")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/671189")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function AddShouldNotFailAfterCodeIsDeleted() As Task
            ' This test attempts to add and remove a method several times via code model,
            ' verifying a scenario where the WinForms or XAML designer adds an event handler
            ' and the user later deletes and regenerates the event handler.

            Dim codeBeforeOperationXml =
<code>
class C
{
}
</code>

            Dim codeAfterOperationXml =
<code>
class C
{
    void M(int x, int y)
    {

    }
}
</code>

            Dim codeBeforeOperation = codeBeforeOperationXml.Value.NormalizeLineEndings().Trim()
            Dim codeAfterOperation = codeAfterOperationXml.Value.NormalizeLineEndings().Trim()

            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(codeBeforeOperationXml))
                Dim workspace = state.VisualStudioWorkspace
                Dim fileCodeModel = state.FileCodeModel
                Assert.NotNull(fileCodeModel)

                For i = 1 To 10
                    Dim docId = workspace.CurrentSolution.Projects(0).DocumentIds(0)
                    Dim textBeforeOperation = Await workspace.CurrentSolution.GetDocument(docId).GetTextAsync()
                    Assert.Equal(codeBeforeOperation, textBeforeOperation.ToString())

                    Dim classC = TryCast(fileCodeModel.CodeElements.Item(1), EnvDTE.CodeClass)
                    Assert.NotNull(classC)
                    Assert.Equal("C", classC.Name)

                    fileCodeModel.BeginBatch()
                    Dim newFunction = classC.AddFunction("M", EnvDTE.vsCMFunction.vsCMFunctionFunction, Type:=EnvDTE.vsCMTypeRef.vsCMTypeRefVoid, , EnvDTE.vsCMAccess.vsCMAccessDefault)
                    Dim param1 = newFunction.AddParameter("x", EnvDTE.vsCMTypeRef.vsCMTypeRefInt, Position:=-1)
                    Dim param2 = newFunction.AddParameter("y", EnvDTE.vsCMTypeRef.vsCMTypeRefInt, Position:=-1)
                    fileCodeModel.EndBatch()

                    Dim solution = workspace.CurrentSolution
                    Dim textAfterOperation = Await solution.GetDocument(docId).GetTextAsync()
                    Assert.Equal(codeAfterOperation, textAfterOperation.ToString())

                    Dim newText = textAfterOperation.Replace(
                        span:=TextSpan.FromBounds(0, textAfterOperation.Length),
                        newText:=codeBeforeOperation)

                    Dim newSolution = solution.WithDocumentText(docId, newText)
                    Assert.True(workspace.TryApplyChanges(newSolution))
                Next
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/31735")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RenameShouldWorkAndElementsShouldBeUsableAfter()
            Dim code =
<code>
class C
{
}
</code>

            TestOperation(code,
                Sub(fileCodeModel)
                    Dim codeElement = DirectCast(fileCodeModel.CodeElements(0), EnvDTE80.CodeElement2)
                    Assert.Equal("C", codeElement.Name)

                    codeElement.RenameSymbol("D")

                    ' This not only asserts that the rename happened successfully, but that the element was correctly updated
                    ' so the underlying node key is still valid.
                    Assert.Equal("D", codeElement.Name)
                End Sub)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub CanAccessPartialPartsInSourceGeneratedFiles()
            Dim code =
<Workspace>
    <Project Language=<%= LanguageName %> CommonReferences="true">
        <Document>partial class C { }</Document>
        <DocumentFromSourceGenerator>partial class C { void M() { } }</DocumentFromSourceGenerator>
    </Project>
</Workspace>

            Using state = CreateCodeModelTestState(code)
                Dim workspace = state.VisualStudioWorkspace
                Dim fileCodeModel = state.FileCodeModel

                Dim codeClass = DirectCast(Assert.Single(fileCodeModel.CodeElements()), EnvDTE80.CodeClass2)
                Dim parts = codeClass.Parts.Cast(Of EnvDTE.CodeClass).ToList()
                Assert.Equal(2, parts.Count)

                ' Grab the part that isn't the one we started with
                Dim generatedPart = Assert.Single(parts, Function(p) p IsNot codeClass)

                ' Confirm we can inspect members
                Dim member = DirectCast(Assert.Single(generatedPart.Members), EnvDTE.CodeFunction)
                Assert.Equal("M", member.Name)

                ' We are unable to change things in generated files
                Assert.Throws(Of COMException)(Sub() member.AddParameter("Test", "System.Object"))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub RequestingCodeModelForAdditionalFileThrowsCorrectExceptionType()
            Dim code =
<Workspace>
    <Project Language=<%= LanguageName %> CommonReferences="true">
        <AdditionalDocument FilePath="Z:\Additional.txt"></AdditionalDocument>
    </Project>
</Workspace>

            Using state = CreateCodeModelTestState(code)
                Dim workspace = state.VisualStudioWorkspace
                Dim projectCodeModelFactory = state.Workspace.GetService(Of ProjectCodeModelFactory)()
                Dim projectCodeModel = projectCodeModelFactory.GetProjectCodeModel(workspace.CurrentSolution.ProjectIds.Single())

                Assert.Throws(Of NotImplementedException)(
                    Sub()
                        projectCodeModel.GetOrCreateFileCodeModel("Z:\Additional.txt", parent:=Nothing)
                    End Sub)
            End Using
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
