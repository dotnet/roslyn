' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CSharpFileCodeModelTests
        Inherits AbstractFileCodeModelTests

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

class C { }</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

class C { }</Code>

            TestAddAttribute(code, expected, New AttributeData With {.Name = "System.CLSCompliant", .Value = "true", .Position = -1})
        End Sub

#End Region

#Region "AddClass tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

#End Region

#Region "AddDelegate tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClosedDocument()
            Dim code =
<Code>
class $$C
{
    void M() { }
}
</Code>
            Using workspaceAndFileCodeModel = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeClass = workspaceAndFileCodeModel.GetCodeElementAtCursor(Of EnvDTE80.CodeClass2)
                Assert.Equal(1, codeClass.Members.OfType(Of EnvDTE80.CodeFunction2)().Count())
                Dim project = workspaceAndFileCodeModel.VisualStudioWorkspace.CurrentSolution.Projects.First()
                Dim documentId = project.DocumentIds.First()
                workspaceAndFileCodeModel.VisualStudioWorkspace.CloseDocument(documentId)
                Dim newSolution = workspaceAndFileCodeModel.VisualStudioWorkspace.CurrentSolution.RemoveDocument(documentId)
                workspaceAndFileCodeModel.VisualStudioWorkspace.TryApplyChanges(newSolution)
                ' throws COMExpection with HResult = E_FAIL
                Assert.Throws(Of System.Runtime.InteropServices.COMException)(
                    Sub()
                        Dim count = codeClass.Members.OfType(Of EnvDTE80.CodeFunction2)().Count()
                    End Sub)
            End Using
        End Sub

        <WorkItem(1980, "https://github.com/dotnet/roslyn/issues/1980")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
                    Assert.Equal(unknownCodeFunction.Name, "operator implicit D")
                End Using
            End Using
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
