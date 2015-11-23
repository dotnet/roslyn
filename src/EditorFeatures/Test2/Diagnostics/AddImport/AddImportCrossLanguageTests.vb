' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport
Imports Microsoft.CodeAnalysis.Diagnostics
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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
    End Class
End Namespace
