' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddImport

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.AddImport

    Public Class AddImportCrossLanguageTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Dim fixer As CodeFixProvider
            If language = LanguageNames.CSharp Then
                fixer = New CSharpAddImportCodeFixProvider()
            Else
                fixer = New VisualBasicAddImportCodeFixProvider()
            End If

            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, fixer)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function Test_CSharpToVisualBasic1() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <ProjectReference>VBAssembly1</ProjectReference>
                        <Document FilePath="Test1.vb">
                            public class Class1
                            {
                                public void Foo()
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
                                public void Foo()
                                {
                                    var x = new Class2();
                                }
                            }
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function Test_VisualBasicToCSharp1() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <Document FilePath="Test1.vb">
                            public class Class1
                                public sub Foo()
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
                                Public Sub Foo()
                                    Dim x As New Class2()
                                End Sub
                            End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(1083419)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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

        <WorkItem(1083419)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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

            Await TestAsync(input, expected, codeActionIndex:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddProjectReference_CSharpToCSharp() As Task
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

            Await TestAsync(input, expected, codeActionIndex:=0, addedReference:="CSAssembly1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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

            Await TestAsync(input, expected, codeActionIndex:=0, addedReference:="VBAssembly1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        <WorkItem(8036, "https://github.com/dotnet/Roslyn/issues/8036")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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


        Protected Overloads Async Function TestAsync(definition As XElement,
                           Optional expected As String = Nothing,
                           Optional codeActionIndex As Integer = 0,
                           Optional addedReference As String = Nothing) As Task
            Dim verifySolutions As Action(Of Solution, Solution) = Nothing
            If addedReference IsNot Nothing Then
                verifySolutions =
                    Sub(oldSolution As Solution, newSolution As Solution)
                        Dim changedDoc = SolutionUtilities.GetSingleChangedDocument(oldSolution, newSolution)
                        Dim oldProject = oldSolution.GetDocument(changedDoc.Id).Project
                        Dim newProject = newSolution.GetDocument(changedDoc.Id).Project

                        Dim oldProjectReferences = From r In oldProject.ProjectReferences
                                                   Let p = oldSolution.GetProject(r.ProjectId)
                                                   Select p.Name
                        Assert.False(oldProjectReferences.Contains(addedReference))

                        Dim newProjectReferences = From r In newProject.ProjectReferences
                                                   Let p = newSolution.GetProject(r.ProjectId)
                                                   Select p.Name

                        Assert.True(newProjectReferences.Contains(addedReference))
                    End Sub
            End If

            Await TestAsync(definition, expected, codeActionIndex, verifySolutions:=verifySolutions)
        End Function
    End Class
End Namespace
