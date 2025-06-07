' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

    <[UseExportProvider]>
    Public Class SymbolDescriptionServiceTests

        Private Shared Async Function TestAsync(languageServiceProvider As HostLanguageServices, workspace As EditorTestWorkspace, expectedDescription As String) As Task

            Dim solution = workspace.CurrentSolution
            Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
            Dim cursorPosition = cursorDocument.CursorPosition.Value
            Dim cursorBuffer = cursorDocument.GetTextBuffer()

            Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
            Dim semanticModel = Await document.GetSemanticModelAsync()
            Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, cursorPosition)

            Dim symbolDescriptionService = languageServiceProvider.GetService(Of ISymbolDisplayService)()

            Dim options = SymbolDescriptionOptions.Default
            Dim actualDescription = Await symbolDescriptionService.ToDescriptionStringAsync(semanticModel, cursorPosition, symbol, options)

            Assert.Equal(expectedDescription, actualDescription)

        End Function

        Private Shared Function StringFromLines(ParamArray lines As String()) As String
            Return String.Join(Environment.NewLine, lines)
        End Function

        Private Shared Async Function TestCSharpAsync(workspaceDefinition As XElement, expectedDescription As String) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceDefinition)
                Await TestAsync(GetLanguageServiceProvider(workspace, LanguageNames.CSharp), workspace, expectedDescription)
            End Using
        End Function

        Private Shared Async Function TestBasicAsync(workspaceDefinition As XElement, expectedDescription As String) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceDefinition)
                Await TestAsync(GetLanguageServiceProvider(workspace, LanguageNames.VisualBasic), workspace, expectedDescription)
            End Using
        End Function

        Private Shared Function GetLanguageServiceProvider(workspace As EditorTestWorkspace, language As String) As HostLanguageServices
            Return workspace.Services.GetLanguageServices(language)
        End Function

        Private Shared Function WrapCodeInWorkspace(ParamArray lines As String()) As XElement
            Dim part1 = "<Workspace> <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true""> <Document>"
            Dim part2 = "</Document></Project></Workspace>"
            Dim code = StringFromLines(lines)
            Dim workspace = String.Concat(part1, code, part2)
            WrapCodeInWorkspace = XElement.Parse(workspace)
        End Function

#Region "CSharp SymbolDescription Tests"

        <Fact>
        Public Async Function TestCSharpDynamic() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Goo { void M() { dyn$$amic d; } }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace,
                       StringFromLines("dynamic",
                                       FeaturesResources.Represents_an_object_whose_operations_will_be_resolved_at_runtime))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543912")>
        Public Async Function TestCSharpLocalConstant() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Goo
            {
                void Method()
                {
                    const int $$x = 2
                }
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.local_constant}) int x = 2")
        End Function

        <Fact>
        Public Async Function TestCSharpStaticField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo
            {
                private static int $$x;
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.field}) static int Foo.x")
        End Function

        <Fact>
        Public Async Function TestCSharpReadOnlyField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo
            {
                private readonly int $$x;
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.field}) readonly int Foo.x")
        End Function

        <Fact>
        Public Async Function TestCSharpVolatileField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo
            {
                private volatile int $$x;
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.field}) volatile int Foo.x")
        End Function

        <Fact>
        Public Async Function TestCSharpStaticReadOnlyField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo
            {
                private static readonly int $$x;
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.field}) static readonly int Foo.x")
        End Function

        <Fact>
        Public Async Function TestCSharpStaticVolatileField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo
            {
                private static volatile int $$x;
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.field}) static volatile int Foo.x")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33049")>
        Public Async Function TestCSharpDefaultParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            using System.Threading;
            class Goo
            {
                void Method(CancellationToken cancellationToken = default(CancellationToken))
                {
                    $$Method(CancellationToken.None);
                }
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"void Goo.Method([CancellationToken cancellationToken = default])")
        End Function

#End Region

#Region "Basic SymbolDescription Tests"

        <Fact>
        Public Async Function TestNamedTypeKindClass() As Task
            Dim workspace = WrapCodeInWorkspace("class Program",
                                                "Dim p as Prog$$ram",
                                                "End class")
            Await TestBasicAsync(workspace, "Class Program")
        End Function

        ''' <summary>
        ''' Design Change from Dev10. Notice that we now show the type information for T
        ''' C# / VB Quick Info consistency
        ''' </summary>
        ''' <remarks></remarks>
        <Fact>
        Public Async Function TestGenericClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            class Program
                Dim p as New System.Collections.Generic.Lis$$t(Of String)
            End class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace,
                        StringFromLines("Sub List(Of String).New()"))
        End Function

        <Fact>
        Public Async Function TestGenericClassFromSource() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Namespace TestNamespace
                Public Class Outer(Of T)
                End Class

                Module Test
                    Dim x As New O$$uter(Of Integer)
                End Module
            End Namespace
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace,
                        StringFromLines("Sub Outer(Of Integer).New()"))
        End Function

        <Fact>
        Public Async Function TestClassNestedWithinAGenericClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Public Class Outer(Of T)
                Public Class Inner
                    Public Sub F(x As T)
                    End Sub
                End Class
            End Class

            Module Test
                Sub Main()
                    Dim x As New Outer(Of Integer).In$$ner()
                    x.F(4)
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace,
                      StringFromLines("Sub Outer(Of Integer).Inner.New()"))
        End Function

        <Fact>
        Public Async Function TestTypeParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Public Class Goo(Of T)
                Dim x as T$$
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"T {FeaturesResources.in_} Goo(Of T)")
        End Function

        <Fact>
        Public Async Function TestTypeParameterFromNestedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Public Class Outer(Of T)
                Public Class Inner
                    Public Sub F(x As T$$)
                    End Sub
                End Class
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"T {FeaturesResources.in_} Outer(Of T)")
        End Function

        <Fact>
        Public Async Function TestShadowedTypeParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Public Class Outer(Of T)
                Public Class Inner
                    Public Sub F(x As T$$)
                    End Sub
                End Class
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"T {FeaturesResources.in_} Outer(Of T)")
        End Function

        <Fact>
        Public Async Function TestNullableOfInt() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Imports System
            Public Class Goo
                Dim x as Nullab$$le(Of Integer)
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace,
                      StringFromLines("Structure System.Nullable(Of T As Structure)",
                                      String.Empty,
                                      $"T {FeaturesResources.is_} Integer"))
        End Function

        <Fact>
        Public Async Function TestDictionaryOfIntAndString() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Imports System.Collections.Generic
            class Program
                Dim p as New Dictio$$nary(Of Integer, String)
            End class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace,
                        StringFromLines("Sub Dictionary(Of Integer, String).New()"))
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindStructure() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Structure Program
                Dim p as Prog$$ram
            End Structure
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Structure Program")
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindStructureBuiltIn() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            class Program
                Dim p as Int$$eger
            End class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Structure System.Int32")
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindEnum() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Enum Program

            End Enum

            Module M1
                Sub Main(args As String())
                    Dim p as Prog$$ram
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Enum Program")
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindDelegate() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Delegate Sub DelegateType()

            Module M1
                Event AnEvent As Delega$$teType
                Sub Main(args As String())

                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Delegate Sub DelegateType()")
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindInterface() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Interface Goo

            End Interface

            Module M1
                Sub Main(args As String())
                    Dim p as Goo$$
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Interface Goo")
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindModule() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                sub Method()
                    $$M1.M()
                End sub
            End Class

            Module M1
                public sub M()

                End sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Module M1")
        End Function

        <Fact>
        Public Async Function TestNamespace() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                sub Method()
                    Sys$$tem.Console.Write(5)
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Namespace System")
        End Function

        <Fact>
        Public Async Function TestNamespace2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
           Imports System.Collections.Gene$$ric
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Namespace System.Collections.Generic")
        End Function

        <Fact>
        Public Async Function TestField() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                private field as Integer
                sub Method()
                    fie$$ld = 5
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.field}) Goo.field As Integer")
        End Function

        <Fact>
        Public Async Function TestSharedField() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                private Shared field as Integer
                sub Method()
                    fie$$ld = 5
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.field}) Shared Goo.field As Integer")
        End Function

        <Fact>
        Public Async Function TestReadOnlyField() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                private ReadOnly field as Integer
                sub Method()
                    fie$$ld = 5
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.field}) ReadOnly Goo.field As Integer")
        End Function

        <Fact>
        Public Async Function TestSharedReadOnlyField() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                private Shared ReadOnly field as Integer
                sub Method()
                    fie$$ld = 5
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.field}) Shared ReadOnly Goo.field As Integer")
        End Function

        <Fact>
        Public Async Function TestLocal() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                sub Method()
                    Dim x as String
                    x$$ = "Hello"
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.local_variable}) x As String")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538732")>
        Public Async Function TestMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                Sub Method()
                    Fu$$n()
                End Sub
                Function Fun() As Integer
                    Return 1
                End Function
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Function Goo.Fun() As Integer")
        End Function

        ''' <summary>
        ''' This is a design change from Dev10. Notice that modifiers "public shared sub" are absent.
        ''' VB / C# Quick Info Consistency
        ''' </summary>
        ''' <remarks></remarks>
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538732")>
        Public Async Function TestPEMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                Sub Method()
                    System.Console.Writ$$e(5)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Sub Console.Write(value As Integer)")
        End Function

        ''' <summary>
        ''' This is a design change from Dev10. Showing what we already know is kinda useless.
        ''' This is what C# does. We are modifying VB to follow this model.
        ''' </summary>
        ''' <remarks></remarks>
        <Fact>
        Public Async Function TestFormalParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                Sub Method()
                End Sub
                Function Fun(x$$ As String) As Integer
                    Return 1
                End Function
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.parameter}) x As String")
        End Function

        <Fact>
        Public Async Function TestOptionalParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                Sub Method(x As Short, Optional y As Integer = 10)
                End Sub
                Sub Test
                    Met$$hod(1, 2)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Sub Goo.Method(x As Short, [y As Integer = 10])")
        End Function

        <Fact>
        Public Async Function TestOverloadedMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                Overloads Sub Method(x As Integer)
                End Sub

                Overloads Sub Method(x As String)
                End Sub

                Sub Test()
                    Meth$$od("str")
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Sub Goo.Method(x As String)")
        End Function

        <Fact>
        Public Async Function TestOverloadedMethods() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                Overloads Sub Method(x As Integer)
                End Sub

                Overloads Sub Method(x As String)
                End Sub

                Overloads Sub Method(x As Double)
                End Sub

                Sub Test()
                    Meth$$od("str")
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Sub Goo.Method(x As String)")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        Public Async Function TestInterfaceConstraintOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Class CC(Of T$$ As IEnumerable(Of Integer))",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.in_} CC(Of T As IEnumerable(Of Integer))"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        Public Async Function TestInterfaceConstraintOnInterface() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Interface IMyInterface(Of T$$ As IEnumerable(Of Integer))",
                                                "End Interface")
            Dim expectedDescription = $"T {FeaturesResources.in_} IMyInterface(Of T As IEnumerable(Of Integer))"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        Public Async Function TestReferenceTypeConstraintOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Class CC(Of T$$ As Class)",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.in_} CC(Of T As Class)"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        Public Async Function TestValueTypeConstraintOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Class CC(Of T$$ As Structure)",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.in_} CC(Of T As Structure)"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        Public Async Function TestValueTypeConstraintOnStructure() As Task
            Dim workspace = WrapCodeInWorkspace("Structure S(Of T$$ As Class)",
                                                "End Structure")
            Dim expectedDescription = $"T {FeaturesResources.in_} S(Of T As Class)"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        Public Async Function TestMultipleConstraintsOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Public Class CC(Of T$$ As {IComparable, IDisposable, Class, New})",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.in_} CC(Of T As {{Class, IComparable, IDisposable, New}})"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        ''' TO DO: Add test for Ref Arg
        <Fact>
        Public Async Function TestOutArguments() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Class CC(Of T As IEnum$$erable(Of Integer))",
                                                "End Class")
            Dim expectedDescription = StringFromLines("Interface System.Collections.Generic.IEnumerable(Of Out T)",
                                                      String.Empty,
                                                      $"T {FeaturesResources.is_} Integer")
            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527655")>
        Public Async Function TestMinimalDisplayName() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System",
                                                "Imports System.Collections.Generic",
                                                "Class CC(Of T As IEnu$$merable(Of IEnumerable(of Int32)))",
                                                "End Class")
            Dim expectedDescription = StringFromLines("Interface System.Collections.Generic.IEnumerable(Of Out T)",
                                                      String.Empty,
                                                      $"T {FeaturesResources.is_} IEnumerable(Of Integer)")
            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <Fact>
        Public Async Function TestOverridableMethod() As Task
            Dim workspace =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
            Class A
                Public Overridable Sub G()
                End Sub
            End Class

            Class B
                Inherits A
                Public Overrides Sub G()
                End Sub
            End Class

            Class C
                Sub Test()
                    Dim x As A
                    x = new A()
                    x.G$$()
                End Sub
            End Class
        </Document>
        </Project>
    </Workspace>
            Await TestBasicAsync(workspace, "Sub A.G()")
        End Function

        <Fact>
        Public Async Function TestOverriddenMethod2() As Task
            Dim workspace =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
            Class A
                Public Overridable Sub G()
                End Sub
            End Class

            Class B
                Inherits A
                Public Overrides Sub G()
                End Sub
            End Class

            Class C
                Sub Test()
                    Dim x As A
                    x = new B()
                    x.G$$()
                End Sub
            End Class
        </Document>
        </Project>
    </Workspace>
            Await TestBasicAsync(workspace, "Sub A.G()")
        End Function

        <Fact>
        Public Async Function TestGenericMethod() As Task
            Dim workspace =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
            Public Class Outer(Of T)
                Public Class Inner
                    Public Sub F(x As T)
                    End Sub
                End Class
            End Class

            Module Test
                Sub Main()
                    Dim x As New Outer(Of Integer).Inner()
                    x.F$$(4)
                End Sub
            End Module
        </Document>
        </Project>
    </Workspace>
            Await TestBasicAsync(workspace, "Sub Outer(Of Integer).Inner.F(x As Integer)")
        End Function

        <Fact>
        Public Async Function TestAutoImplementedProperty() As Task
            Dim workspace =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
            Imports System.Collections.Generic
            Class Goo
                Public Property It$$ems As New List(Of String) From {"M", "T", "W"}
            End Class
        </Document>
        </Project>
    </Workspace>
            Await TestBasicAsync(workspace, "Property Goo.Items As List(Of String)")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538806")>
        Public Async Function TestField1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class C
                Dim x As Integer
                Sub Method()
                    Dim y As Integer
                    $$x = y
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.field}) C.x As Integer")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538806")>
        Public Async Function TestProperty1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class C
                Dim x As Integer
                Sub Method()
                    Dim y As Integer
                    x = $$y
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.local_variable}) y As Integer")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543911")>
        Public Async Function TestVBLocalConstant() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Goo
                sub Method()
                    Const $$b = 2
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.local_constant}) b As Integer = 2")
        End Function

#End Region

    End Class
End Namespace
