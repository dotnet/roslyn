' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

    Public Class SymbolDescriptionServiceTests

        Private Async Function TestAsync(languageServiceProvider As HostLanguageServices, workspace As TestWorkspace, expectedDescription As String) As Task

            Dim solution = workspace.CurrentSolution
            Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
            Dim cursorPosition = cursorDocument.CursorPosition.Value
            Dim cursorBuffer = cursorDocument.TextBuffer

            Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

            ' using GetTouchingWord instead of FindToken allows us to test scenarios where cursor is at the end of token (E.g: Foo$$)
            Dim tree = Await document.GetSyntaxTreeAsync()
            Dim commonSyntaxToken = Await tree.GetTouchingWordAsync(cursorPosition, languageServiceProvider.GetService(Of ISyntaxFactsService), Nothing)

            ' For String Literals GetTouchingWord returns Nothing, we still need this for Quick Info. Quick Info code does exactly the following.
            ' caveat: The comment above the previous line of code. Do not put the cursor at the end of the token.
            If commonSyntaxToken = Nothing Then
                commonSyntaxToken = (Await document.GetSyntaxRootAsync()).FindToken(cursorPosition)
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync()
            Dim symbol = semanticModel.GetSymbols(commonSyntaxToken, document.Project.Solution.Workspace, bindLiteralsToUnderlyingType:=True, cancellationToken:=CancellationToken.None).AsImmutable()
            Dim symbolDescriptionService = languageServiceProvider.GetService(Of ISymbolDisplayService)()

            Dim actualDescription = Await symbolDescriptionService.ToDescriptionStringAsync(workspace, semanticModel, cursorPosition, symbol)

            Assert.Equal(expectedDescription, actualDescription)

        End Function

        Private Function StringFromLines(ParamArray lines As String()) As String
            Return String.Join(Environment.NewLine, lines)
        End Function

        Private Async Function TestCSharpAsync(workspaceDefinition As XElement, expectedDescription As String) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Await TestAsync(GetLanguageServiceProvider(workspace, LanguageNames.CSharp), workspace, expectedDescription)
            End Using
        End Function

        Private Async Function TestBasicAsync(workspaceDefinition As XElement, expectedDescription As String) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Await TestAsync(GetLanguageServiceProvider(workspace, LanguageNames.VisualBasic), workspace, expectedDescription)
            End Using
        End Function

        Private Function GetLanguageServiceProvider(workspace As TestWorkspace, language As String) As HostLanguageServices
            Return workspace.Services.GetLanguageServices(language)
        End Function

        Private Function WrapCodeInWorkspace(ParamArray lines As String()) As XElement
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
            class Foo { void M() { dyn$$amic d; } }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace,
                       StringFromLines("dynamic",
                                       FeaturesResources.RepresentsAnObjectWhoseOperations))
        End Function

        <WorkItem(543912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543912")>
        <Fact>
        Public Async Function TestCSharpLocalConstant() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo
            {
                void Method()
                {
                    const int $$x = 2
                }
            }
        </Document>
    </Project>
</Workspace>
            Await TestCSharpAsync(workspace, $"({FeaturesResources.LocalConstant}) int x = 2")
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
            Public Class Foo(Of T)
                Dim x as T$$
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"T {FeaturesResources.In} Foo(Of T)")
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
            Await TestBasicAsync(workspace, $"T {FeaturesResources.In} Outer(Of T)")
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
            Await TestBasicAsync(workspace, $"T {FeaturesResources.In} Outer(Of T)")
        End Function

        <Fact>
        Public Async Function TestNullableOfInt() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Imports System
            Public Class Foo
                Dim x as Nullab$$le(Of Integer)
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace,
                      StringFromLines("Structure System.Nullable(Of T As Structure)",
                                      String.Empty,
                                      $"T {FeaturesResources.Is} Integer"))
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
            Interface Foo

            End Interface

            Module M1
                Sub Main(args As String())
                    Dim p as Foo$$
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Interface Foo")
        End Function

        <Fact>
        Public Async Function TestNamedTypeKindModule() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
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
            Class Foo
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
            Class Foo
                private field as Integer
                sub Method()
                    fie$$ld = 5
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.Field}) Foo.field As Integer")
        End Function

        <Fact>
        Public Async Function TestLocal() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                sub Method()
                    Dim x as String
                    x$$ = "Hello"
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.LocalVariable}) x As String")
        End Function

        <Fact>
        Public Async Function TestStringLiteral() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                Sub Method()
                    Dim x As String = "Hel$$lo"
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Class System.String")
        End Function

        <Fact>
        Public Async Function TestIntegerLiteral() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                Sub Method()
                    Dim x = 4$$2
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Structure System.Int32")
        End Function

        <Fact>
        Public Async Function TestDateLiteral() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                Sub Method()
                       Dim d As Date
                       d = #8/23/1970 $$3:45:39 AM#
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Structure System.DateTime")
        End Function

        ''' Design change from Dev10
        <Fact>
        Public Async Function TestNothingLiteral() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                Sub Method()
                    Dim x = Nothin$$g
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "")
        End Function

        <Fact>
        Public Async Function TestTrueKeyword() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                Sub Method()
                    Dim x = Tr$$ue
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Structure System.Boolean")
        End Function

        <WorkItem(538732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538732")>
        <Fact>
        Public Async Function TestMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
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
            Await TestBasicAsync(workspace, "Function Foo.Fun() As Integer")
        End Function

        ''' <summary>
        ''' This is a design change from Dev10. Notice that modifiers "public shared sub" are absent.
        ''' VB / C# Quick Info Consistency
        ''' </summary>
        ''' <remarks></remarks>
        <WorkItem(538732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538732")>
        <Fact>
        Public Async Function TestPEMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
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
            Class Foo
                Sub Method()
                End Sub
                Function Fun(x$$ As String) As Integer
                    Return 1
                End Function
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.Parameter}) x As String")
        End Function

        <Fact>
        Public Async Function TestOptionalParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                Sub Method(x As Short, Optional y As Integer = 10)
                End Sub
                Sub Test
                    Met$$hod(1, 2)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, "Sub Foo.Method(x As Short, [y As Integer = 10])")
        End Function

        <Fact>
        Public Async Function TestOverloadedMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
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
            Await TestBasicAsync(workspace, "Sub Foo.Method(x As String)")
        End Function

        <Fact>
        Public Async Function TestOverloadedMethods() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
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
            Await TestBasicAsync(workspace, "Sub Foo.Method(x As String)")
        End Function

        <WorkItem(527639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        <Fact>
        Public Async Function TestInterfaceConstraintOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Class CC(Of T$$ As IEnumerable(Of Integer))",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As IEnumerable(Of Integer))"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <WorkItem(527639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        <Fact>
        Public Async Function TestInterfaceConstraintOnInterface() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Interface IMyInterface(Of T$$ As IEnumerable(Of Integer))",
                                                "End Interface")
            Dim expectedDescription = $"T {FeaturesResources.In} IMyInterface(Of T As IEnumerable(Of Integer))"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <WorkItem(527639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        <Fact>
        Public Async Function TestReferenceTypeConstraintOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Class CC(Of T$$ As Class)",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As Class)"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <WorkItem(527639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        <Fact>
        Public Async Function TestValueTypeConstraintOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Class CC(Of T$$ As Structure)",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As Structure)"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <WorkItem(527639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        <Fact>
        Public Async Function TestValueTypeConstraintOnStructure() As Task
            Dim workspace = WrapCodeInWorkspace("Structure S(Of T$$ As Class)",
                                                "End Structure")
            Dim expectedDescription = $"T {FeaturesResources.In} S(Of T As Class)"

            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <WorkItem(527639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527639")>
        <Fact>
        Public Async Function TestMultipleConstraintsOnClass() As Task
            Dim workspace = WrapCodeInWorkspace("Public Class CC(Of T$$ As {IComparable, IDisposable, Class, New})",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As {{Class, IComparable, IDisposable, New}})"

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
                                                      $"T {FeaturesResources.Is} Integer")
            Await TestBasicAsync(workspace, expectedDescription)
        End Function

        <WorkItem(527655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527655")>
        <Fact>
        Public Async Function TestMinimalDisplayName() As Task
            Dim workspace = WrapCodeInWorkspace("Imports System",
                                                "Imports System.Collections.Generic",
                                                "Class CC(Of T As IEnu$$merable(Of IEnumerable(of Int32)))",
                                                "End Class")
            Dim expectedDescription = StringFromLines("Interface System.Collections.Generic.IEnumerable(Of Out T)",
                                                      String.Empty,
                                                      $"T {FeaturesResources.Is} IEnumerable(Of Integer)")
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
            Class Foo
                Public Property It$$ems As New List(Of String) From {"M", "T", "W"}
            End Class
        </Document>
        </Project>
    </Workspace>
            Await TestBasicAsync(workspace, "Property Foo.Items As List(Of String)")
        End Function

        <WorkItem(538806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538806")>
        <Fact>
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
            Await TestBasicAsync(workspace, $"({FeaturesResources.Field}) C.x As Integer")
        End Function

        <WorkItem(538806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538806")>
        <Fact>
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
            Await TestBasicAsync(workspace, $"({FeaturesResources.LocalVariable}) y As Integer")
        End Function

        <WorkItem(543911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543911")>
        <Fact>
        Public Async Function TestVBLocalConstant() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Foo
                sub Method()
                    Const $$b = 2
                End sub
            End Class
        </Document>
    </Project>
</Workspace>
            Await TestBasicAsync(workspace, $"({FeaturesResources.LocalConstant}) b As Integer = 2")
        End Function

#End Region

    End Class
End Namespace
