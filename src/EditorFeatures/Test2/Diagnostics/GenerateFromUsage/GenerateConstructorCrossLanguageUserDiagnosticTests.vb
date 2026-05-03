' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateConstructor
    Partial Public Class GenerateConstructorCrossLanguageUserDiagnosticTests
        Inherits AbstractCrossLanguageUserDiagnosticTests

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As (DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return (Nothing, New CodeAnalysis.CSharp.GenerateConstructor.GenerateConstructorCodeFixProvider())
            Else
                Return (Nothing, New CodeAnalysis.VisualBasic.GenerateConstructor.GenerateConstructorCodeFixProvider())
            End If
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546671")>
        Public Async Function Test_CSharpToVisualBasic1() As System.Threading.Tasks.Task
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Goo()
                        {
                            new $$VBClass("hello");
                        }
                    }
                </Document>
            </Project>
            <Project Language="Visual Basic" AssemblyName="VbAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    Public Class VBClass

                    End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                    Public Class VBClass
                        Private v As String

                        Public Sub New(v As String)
                            Me.v = v
                        End Sub
                    End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

    End Class
End Namespace
