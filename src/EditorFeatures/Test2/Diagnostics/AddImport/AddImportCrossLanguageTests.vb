' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.AddImport
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.FindSymbols.SymbolTree
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport
Imports Roslyn.Utilities
Imports Microsoft.CodeAnalysis.CodeActions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.AddImport
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
    Public Class AddImportCrossLanguageTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As (DiagnosticAnalyzer, CodeFixProvider)
            Dim fixer As CodeFixProvider
            If language = LanguageNames.CSharp Then
                fixer = New CSharpAddImportCodeFixProvider()
            Else
                fixer = New VisualBasicAddImportCodeFixProvider()
            End If

            Return (Nothing, fixer)
        End Function

        <Fact>
        Public Async Function Test_CSharpToVisualBasic1() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <ProjectReference>VBAssembly1</ProjectReference>
                        <Document FilePath="Test1.vb">
                            public class Class1
                            {
                                public void Goo()
                                {
                                    var x = new Cl$$ass2();
                                }
                            }
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <Document FilePath='Test2.vb'>
                            namespace NS2
                                public class Class2
                                end class
                            end namespace
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
                            using NS2;

                            public class Class1
                            {
                                public void Goo()
                                {
                                    var x = new Class2();
                                }
                            }
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function Test_VisualBasicToCSharp1() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document FilePath="Test1.vb">
                            public class Class1
                                public sub Goo()
                                    dim x as new Cl$$ass2()
                                end sub
                            end class
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test2.cs'>
                            namespace NS2
                            {
                                public class Class2
                                {
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
                            Imports NS2

                            Public Class Class1
                                Public Sub Goo()
                                    Dim x As New Class2()
                                End Sub
                            End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1083419")>
        Public Async Function TestExtensionMethods1() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <Document FilePath="Test1.vb">
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Namespace VBAssembly1
    Public Module Module1
        &lt;Extension&gt;
        Public Function [Select](x As List(Of Integer)) As IEnumerable(Of Integer)
            Return Nothing
        End Function
    End Module
End Namespace
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <ProjectReference>VBAssembly1</ProjectReference>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    class Program
    {
        static void Main()
        {
            var l = new List&lt;int&gt;();
            l.Se$$lect();
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
using System.Collections.Generic;
using VBAssembly1;
namespace CSAssembly1
{
    class Program
    {
        static void Main()
        {
            var l = new List&lt;int&gt;();
            l.Select();
        }
    }
}
                </text>.Value.Trim()

            Await TestAsync(input, expected, codeActionIndex:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1083419")>
        Public Async Function TestExtensionMethods2() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    public static class Program
    {
        public static IEnumerable&lt;int&gt; Select(this List&lt;int&gt; x)
        {
            return null;
        }
    }
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test1.vb">
Imports System.Collections.Generic
Namespace VBAssembly1
    Module Module1
        Sub Main()
            Dim l = New List(Of Integer)()
            l.Se$$lect()
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Imports System.Collections.Generic
Imports CSAssembly1
Namespace VBAssembly1
    Module Module1
        Sub Main()
            Dim l = New List(Of Integer)()
            l.Select()
        End Sub
    End Module
End Namespace
                </text>.Value.Trim()

            Await TestAsync(input, expected, codeActionIndex:=1)
        End Function

        <WpfFact>
        Public Async Function AddProjectReference_CSharpToCSharp_Test() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    public class Class1
    {
    }
}
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly2' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test2.cs">
namespace CSAssembly2
{
    public class Class2
    {
        $$Class1 c;
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
using CSAssembly1;

namespace CSAssembly2
{
    public class Class2
    {
        Class1 c;
    }
}
                </text>.Value.Trim()

            Await TestAsync(
                input, expected, codeActionIndex:=0, addedReference:="CSAssembly1",
                glyphTags:=WellKnownTagArrays.CSharpProject.Add(CodeAction.RequiresNonDocumentChange),
                onAfterWorkspaceCreated:=AddressOf WaitForSymbolTreeInfoCache)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12169")>
        Public Async Function AddProjectReference_CSharpToCSharp_StaticField() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    public static class Class1
    {
        public static int StaticField;
    }
}
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly2' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test2.cs">
namespace CSAssembly2
{
    public class Class2
    {
        $$StaticField c;
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Await TestMissing(input)
        End Function

        <WpfFact>
        Public Async Function AddProjectReference_CSharpToCSharp_ExtensionMethod() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    public static class Class1
    {
        public static void Goo(this int x) { }
    }
}
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly2' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test2.cs">
namespace CSAssembly2
{
    public class Class2
    {
        void Bar(int i)
        {
            i.$$Goo();
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
using CSAssembly1;

namespace CSAssembly2
{
    public class Class2
    {
        void Bar(int i)
        {
            i.Goo();
        }
    }
}
                </text>.Value.Trim()

            Await TestAsync(
                input, expected, codeActionIndex:=0, addedReference:="CSAssembly1",
                glyphTags:=WellKnownTagArrays.CSharpProject.Add(CodeAction.RequiresNonDocumentChange),
                onAfterWorkspaceCreated:=AddressOf WaitForSymbolTreeInfoCache)
        End Function

        <WpfFact>
        Public Async Function TestAddProjectReference_CSharpToCSharp_WithProjectRenamed() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    public class Class1
    {
    }
}
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly2' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test2.cs">
namespace CSAssembly2
{
    public class Class2
    {
        $$Class1 c;
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
using CSAssembly1;

namespace CSAssembly2
{
    public class Class2
    {
        Class1 c;
    }
}
                </text>.Value.Trim()

            Await TestAsync(input, expected, codeActionIndex:=0, addedReference:="NewName",
                            glyphTags:=WellKnownTagArrays.CSharpProject.Add(CodeAction.RequiresNonDocumentChange),
                            onAfterWorkspaceCreated:=
                            Async Function(workspace As EditorTestWorkspace)
                                Dim project = workspace.CurrentSolution.Projects.Single(Function(p) p.AssemblyName = "CSAssembly1")
                                workspace.OnProjectNameChanged(project.Id, "NewName", "NewFilePath")
                                Await WaitForSymbolTreeInfoCache(workspace)
                            End Function)
        End Function

        <WpfFact>
        Public Async Function TestAddProjectReference_VBToVB() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.vb'>
Namespace VBAssembly1
    Public Class Class1
    End Class
End Namespace
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly2' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test2.vb">
Namespace VBAssembly2
    Public Class Class2
        dim c As $$Class1
    End Class
End Namespace
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Imports VBAssembly1

Namespace VBAssembly2
    Public Class Class2
        Dim c As Class1
    End Class
End Namespace
                </text>.Value.Trim()

            Await TestAsync(
                input, expected, codeActionIndex:=0, addedReference:="VBAssembly1",
                glyphTags:=WellKnownTagArrays.VisualBasicProject.Add(CodeAction.RequiresNonDocumentChange),
                onAfterWorkspaceCreated:=AddressOf WaitForSymbolTreeInfoCache)
        End Function

        Private Async Function WaitForSymbolTreeInfoCache(workspace As EditorTestWorkspace) As Task
            Dim service = DirectCast(
                workspace.Services.GetRequiredService(Of ISymbolTreeInfoCacheService),
                SymbolTreeInfoCacheServiceFactory.SymbolTreeInfoCacheService)

            Await service.GetTestAccessor().AnalyzeSolutionAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/Roslyn/issues/8036")>
        Public Async Function TestAddProjectReference_CSharpToVB_ExtensionMethod() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.vb'>
Imports System.Runtime.CompilerServices

Namespace N
    Public Module M
        &lt;Extension&gt;
        Public Sub Extension(o As Object)
        End Sub
    End Module
End Namespace
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test1.cs">
class C
{
    void M()
    {
        object o;
        o.$$Extension();
    }
}
                        </Document>
                    </Project>
                </Workspace>

            ' This is not currently supported because we can't find extension methods across
            ' projects of different languge types.  This is due to being unable to 'Reduce'
            ' the extension method properly when the extension method and the receiver are
            ' from different languages (the compilation layer doesn't allow for this).
            '
            ' This test just verifies that we don't crash trying.
            Await TestMissing(input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16022")>
        Public Async Function TestAddProjectReference_EvenWithExistingUsing() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using A;

class C
{
    $$B b;
}
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly2' CommonReferences='true'>
                        <Document FilePath="Test2.cs">
namespace A
{
    public class B { }
}

                        </Document>
                    </Project>
                </Workspace>

            Await TestAsync(input, addedReference:="CSAssembly2",
                            glyphTags:=WellKnownTagArrays.CSharpProject.Add(CodeAction.RequiresNonDocumentChange),
                            onAfterWorkspaceCreated:=AddressOf WaitForSymbolTreeInfoCache)
        End Function

        <Fact>
        Public Async Function TestAddProjectReferenceMissingForCircularReference() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSAssembly1' CommonReferences='true'>
                        <ProjectReference>CSAssembly2</ProjectReference>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;
namespace CSAssembly1
{
    public class Assembly1Class
    {
    }
}
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSAssembly2' CommonReferences='true'>
                        <CompilationOptions></CompilationOptions>
                        <Document FilePath="Test2.cs">
namespace CSAssembly2
{
    public class Class2
    {
        $$Assembly1Class c;
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Await TestMissing(input)
        End Function

        Friend Overloads Async Function TestAsync(definition As XElement,
                                                  Optional expected As String = Nothing,
                                                  Optional codeActionIndex As Integer = 0,
                                                  Optional addedReference As String = Nothing,
                                                  Optional onAfterWorkspaceCreated As Func(Of EditorTestWorkspace, Task) = Nothing,
                                                  Optional glyphTags As ImmutableArray(Of String) = Nothing) As Task
            Dim verifySolutions As Func(Of Solution, Solution, Task) = Nothing
            Dim workspace As EditorTestWorkspace = Nothing

            If addedReference IsNot Nothing Then
                verifySolutions =
                    Function(oldSolution As Solution, newSolution As Solution)
                        Dim initialDocId = workspace.DocumentWithCursor.Id
                        Dim oldProject = oldSolution.GetDocument(initialDocId).Project
                        Dim newProject = newSolution.GetDocument(initialDocId).Project

                        Dim oldProjectReferences = From r In oldProject.ProjectReferences
                                                   Let p = oldSolution.GetProject(r.ProjectId)
                                                   Select p.Name
                        Assert.False(oldProjectReferences.Contains(addedReference))

                        Dim newProjectReferences = From r In newProject.ProjectReferences
                                                   Let p = newSolution.GetProject(r.ProjectId)
                                                   Select p.Name

                        Assert.True(newProjectReferences.Contains(addedReference))
                        Return Task.CompletedTask
                    End Function
            End If

            Await TestAsync(definition, expected, codeActionIndex,
                            verifySolutions:=verifySolutions,
                            glyphTags:=glyphTags,
                            onAfterWorkspaceCreated:=Async Function(ws As EditorTestWorkspace)
                                                         workspace = ws
                                                         If onAfterWorkspaceCreated IsNot Nothing Then
                                                             Await onAfterWorkspaceCreated(ws)
                                                         End If
                                                     End Function)
        End Function
    End Class
End Namespace
