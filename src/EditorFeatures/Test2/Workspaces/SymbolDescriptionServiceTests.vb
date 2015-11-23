' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

    Public Class SymbolDescriptionServiceTests

        Private Sub Test(languageServiceProvider As HostLanguageServices, workspace As TestWorkspace, expectedDescription As String)

            Dim solution = workspace.CurrentSolution
            Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
            Dim cursorPosition = cursorDocument.CursorPosition.Value
            Dim cursorBuffer = cursorDocument.TextBuffer

            Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

            ' using GetTouchingWord instead of FindToken allows us to test scenarios where cursor is at the end of token (E.g: Foo$$)
            Dim commonSyntaxToken = document.GetSyntaxTreeAsync().Result.GetTouchingWord(cursorPosition, languageServiceProvider.GetService(Of ISyntaxFactsService), Nothing)

            ' For String Literals GetTouchingWord returns Nothing, we still need this for Quick Info. Quick Info code does exactly the following.
            ' caveat: The comment above the previous line of code. Do not put the cursor at the end of the token.
            If commonSyntaxToken = Nothing Then
                commonSyntaxToken = document.GetSyntaxTreeAsync().Result.GetRoot().FindToken(cursorPosition)
            End If

            Dim semanticModel = document.GetSemanticModelAsync().Result
            Dim symbol = semanticModel.GetSymbols(commonSyntaxToken, document.Project.Solution.Workspace, bindLiteralsToUnderlyingType:=True, cancellationToken:=CancellationToken.None).AsImmutable()
            Dim symbolDescriptionService = languageServiceProvider.GetService(Of ISymbolDisplayService)()

            Dim actualDescription = symbolDescriptionService.ToDescriptionStringAsync(workspace, semanticModel, cursorPosition, symbol).Result

            Assert.Equal(expectedDescription, actualDescription)

        End Sub

        Private Function StringFromLines(ParamArray lines As String()) As String
            Return String.Join(Environment.NewLine, lines)
        End Function

        Private Sub TestCSharp(workspaceDefinition As XElement, expectedDescription As String)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition)
                Test(GetLanguageServiceProvider(workspace, LanguageNames.CSharp), workspace, expectedDescription)
            End Using
        End Sub

        Private Sub TestBasic(workspaceDefinition As XElement, expectedDescription As String)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition)
                Test(GetLanguageServiceProvider(workspace, LanguageNames.VisualBasic), workspace, expectedDescription)
            End Using
        End Sub

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

        <WpfFact>
        Public Sub TestCSharpDynamic()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Foo { void M() { dyn$$amic d; } }
        </Document>
    </Project>
</Workspace>
            TestCSharp(workspace,
                       StringFromLines("dynamic",
                                       FeaturesResources.RepresentsAnObjectWhoseOperations))
        End Sub

        <WorkItem(543912)>
        <WpfFact>
        Public Sub TestCSharpLocalConstant()
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
            TestCSharp(workspace, $"({FeaturesResources.LocalConstant}) int x = 2")
        End Sub

#End Region

#Region "Basic SymbolDescription Tests"

        <WpfFact>
        Public Sub TestNamedTypeKindClass()
            Dim workspace = WrapCodeInWorkspace("class Program",
                                                "Dim p as Prog$$ram",
                                                "End class")
            TestBasic(workspace, "Class Program")
        End Sub

        ''' <summary>
        ''' Design Change from Dev10. Notice that we now show the type information for T
        ''' C# / VB Quick Info consistency
        ''' </summary>
        ''' <remarks></remarks>
        <WpfFact>
        Public Sub TestGenericClass()
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
            TestBasic(workspace,
                        StringFromLines("Sub List(Of String).New()"))
        End Sub

        <WpfFact>
        Public Sub TestGenericClassFromSource()
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
            TestBasic(workspace,
                        StringFromLines("Sub Outer(Of Integer).New()"))
        End Sub

        <WpfFact>
        Public Sub TestClassNestedWithinAGenericClass()
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
            TestBasic(workspace,
                      StringFromLines("Sub Outer(Of Integer).Inner.New()"))
        End Sub

        <WpfFact>
        Public Sub TestTypeParameter()
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
            TestBasic(workspace, $"T {FeaturesResources.In} Foo(Of T)")
        End Sub

        <WpfFact>
        Public Sub TestTypeParameterFromNestedClass()
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
            TestBasic(workspace, $"T {FeaturesResources.In} Outer(Of T)")
        End Sub

        <WpfFact>
        Public Sub TestShadowedTypeParameter()
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
            TestBasic(workspace, $"T {FeaturesResources.In} Outer(Of T)")
        End Sub

        <WpfFact>
        Public Sub TestNullableOfInt()
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
            TestBasic(workspace,
                      StringFromLines("Structure System.Nullable(Of T As Structure)",
                                      String.Empty,
                                      $"T {FeaturesResources.Is} Integer"))
        End Sub

        <WpfFact>
        Public Sub TestDictionaryOfIntAndString()
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
            TestBasic(workspace,
                        StringFromLines("Sub Dictionary(Of Integer, String).New()"))
        End Sub

        <WpfFact>
        Public Sub TestNamedTypeKindStructure()
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
            TestBasic(workspace, "Structure Program")
        End Sub

        <WpfFact>
        Public Sub TestNamedTypeKindStructureBuiltIn()
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
            TestBasic(workspace, "Structure System.Int32")
        End Sub

        <WpfFact>
        Public Sub TestNamedTypeKindEnum()
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
            TestBasic(workspace, "Enum Program")
        End Sub

        <WpfFact>
        Public Sub TestNamedTypeKindDelegate()
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
            TestBasic(workspace, "Delegate Sub DelegateType()")
        End Sub

        <WpfFact>
        Public Sub TestNamedTypeKindInterface()
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
            TestBasic(workspace, "Interface Foo")
        End Sub

        <WpfFact>
        Public Sub TestNamedTypeKindModule()
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
            TestBasic(workspace, "Module M1")
        End Sub

        <WpfFact>
        Public Sub TestNamespace()
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
            TestBasic(workspace, "Namespace System")
        End Sub

        <WpfFact>
        Public Sub TestNamespace2()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
           Imports System.Collections.Gene$$ric
        </Document>
    </Project>
</Workspace>
            TestBasic(workspace, "Namespace System.Collections.Generic")
        End Sub

        <WpfFact>
        Public Sub TestField()
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
            TestBasic(workspace, $"({FeaturesResources.Field}) Foo.field As Integer")
        End Sub

        <WpfFact>
        Public Sub TestLocal()
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
            TestBasic(workspace, $"({FeaturesResources.LocalVariable}) x As String")
        End Sub

        <WpfFact>
        Public Sub TestStringLiteral()
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
            TestBasic(workspace, "Class System.String")
        End Sub

        <WpfFact>
        Public Sub TestIntegerLiteral()
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
            TestBasic(workspace, "Structure System.Int32")
        End Sub

        <WpfFact>
        Public Sub TestDateLiteral()
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
            TestBasic(workspace, "Structure System.DateTime")
        End Sub

        ''' Design change from Dev10
        <WpfFact>
        Public Sub TestNothingLiteral()
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
            TestBasic(workspace, "")
        End Sub

        <WpfFact>
        Public Sub TestTrueKeyword()
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
            TestBasic(workspace, "Structure System.Boolean")
        End Sub

        <WorkItem(538732)>
        <WpfFact>
        Public Sub TestMethod()
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
            TestBasic(workspace, "Function Foo.Fun() As Integer")
        End Sub

        ''' <summary>
        ''' This is a design change from Dev10. Notice that modifiers "public shared sub" are absent.
        ''' VB / C# Quick Info Consistency
        ''' </summary>
        ''' <remarks></remarks>
        <WorkItem(538732)>
        <WpfFact>
        Public Sub TestPEMethod()
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
            TestBasic(workspace, "Sub Console.Write(value As Integer)")
        End Sub

        ''' <summary>
        ''' This is a design change from Dev10. Showing what we already know is kinda useless.
        ''' This is what C# does. We are modifying VB to follow this model.
        ''' </summary>
        ''' <remarks></remarks>
        <WpfFact>
        Public Sub TestFormalParameter()
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
            TestBasic(workspace, $"({FeaturesResources.Parameter}) x As String")
        End Sub

        <WpfFact>
        Public Sub TestOptionalParameter()
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
            TestBasic(workspace, "Sub Foo.Method(x As Short, [y As Integer = 10])")
        End Sub

        <WpfFact>
        Public Sub TestOverloadedMethod()
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
            TestBasic(workspace, "Sub Foo.Method(x As String)")
        End Sub

        <WpfFact>
        Public Sub TestOverloadedMethods()
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
            TestBasic(workspace, "Sub Foo.Method(x As String)")
        End Sub

        <WorkItem(527639)>
        <WpfFact>
        Public Sub TestInterfaceConstraintOnClass()
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Class CC(Of T$$ As IEnumerable(Of Integer))",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As IEnumerable(Of Integer))"

            TestBasic(workspace, expectedDescription)
        End Sub

        <WorkItem(527639)>
        <WpfFact>
        Public Sub TestInterfaceConstraintOnInterface()
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Interface IMyInterface(Of T$$ As IEnumerable(Of Integer))",
                                                "End Interface")
            Dim expectedDescription = $"T {FeaturesResources.In} IMyInterface(Of T As IEnumerable(Of Integer))"

            TestBasic(workspace, expectedDescription)
        End Sub

        <WorkItem(527639)>
        <WpfFact>
        Public Sub TestReferenceTypeConstraintOnClass()
            Dim workspace = WrapCodeInWorkspace("Class CC(Of T$$ As Class)",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As Class)"

            TestBasic(workspace, expectedDescription)
        End Sub

        <WorkItem(527639)>
        <WpfFact>
        Public Sub TestValueTypeConstraintOnClass()
            Dim workspace = WrapCodeInWorkspace("Class CC(Of T$$ As Structure)",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As Structure)"

            TestBasic(workspace, expectedDescription)
        End Sub

        <WorkItem(527639)>
        <WpfFact>
        Public Sub TestValueTypeConstraintOnStructure()
            Dim workspace = WrapCodeInWorkspace("Structure S(Of T$$ As Class)",
                                                "End Structure")
            Dim expectedDescription = $"T {FeaturesResources.In} S(Of T As Class)"

            TestBasic(workspace, expectedDescription)
        End Sub

        <WorkItem(527639)>
        <WpfFact>
        Public Sub TestMultipleConstraintsOnClass()
            Dim workspace = WrapCodeInWorkspace("Public Class CC(Of T$$ As {IComparable, IDisposable, Class, New})",
                                                "End Class")
            Dim expectedDescription = $"T {FeaturesResources.In} CC(Of T As {{Class, IComparable, IDisposable, New}})"

            TestBasic(workspace, expectedDescription)
        End Sub

        ''' TO DO: Add test for Ref Arg
        <WpfFact>
        Public Sub TestOutArguments()
            Dim workspace = WrapCodeInWorkspace("Imports System.Collections.Generic",
                                                "Class CC(Of T As IEnum$$erable(Of Integer))",
                                                "End Class")
            Dim expectedDescription = StringFromLines("Interface System.Collections.Generic.IEnumerable(Of Out T)",
                                                      String.Empty,
                                                      $"T {FeaturesResources.Is} Integer")
            TestBasic(workspace, expectedDescription)
        End Sub

        <WorkItem(527655)>
        <WpfFact>
        Public Sub TestMinimalDisplayName()
            Dim workspace = WrapCodeInWorkspace("Imports System",
                                                "Imports System.Collections.Generic",
                                                "Class CC(Of T As IEnu$$merable(Of IEnumerable(of Int32)))",
                                                "End Class")
            Dim expectedDescription = StringFromLines("Interface System.Collections.Generic.IEnumerable(Of Out T)",
                                                      String.Empty,
                                                      $"T {FeaturesResources.Is} IEnumerable(Of Integer)")
            TestBasic(workspace, expectedDescription)
        End Sub

        <WpfFact>
        Public Sub TestOverridableMethod()
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
            TestBasic(workspace, "Sub A.G()")
        End Sub

        <WpfFact>
        Public Sub TestOverriddenMethod2()
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
            TestBasic(workspace, "Sub A.G()")
        End Sub

        <WpfFact>
        Public Sub TestGenericMethod()
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
            TestBasic(workspace, "Sub Outer(Of Integer).Inner.F(x As Integer)")
        End Sub

        <WpfFact>
        Public Sub TestAutoImplementedProperty()
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
            TestBasic(workspace, "Property Foo.Items As List(Of String)")
        End Sub

        <WorkItem(538806)>
        <WpfFact>
        Public Sub TestField1()
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
            TestBasic(workspace, $"({FeaturesResources.Field}) C.x As Integer")
        End Sub

        <WorkItem(538806)>
        <WpfFact>
        Public Sub TestProperty1()
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
            TestBasic(workspace, $"({FeaturesResources.LocalVariable}) y As Integer")
        End Sub

        <WorkItem(543911)>
        <WpfFact>
        Public Sub TestVBLocalConstant()
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
            TestBasic(workspace, $"({FeaturesResources.LocalConstant}) b As Integer = 2")
        End Sub

#End Region

    End Class
End Namespace
