' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateConstructor
    Partial Public Class GenerateConstructorCrossLanguageTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateConstructor.GenerateConstructorCodeFixProvider())
            Else
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateConstructor.GenerateConstructorCodeFixProvider())
            End If
        End Function

        <WorkItem(546671)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub Test_CSharpToVisualBasic1()
            Dim input =
        <Workspace>
            <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
                <ProjectReference>VbAssembly1</ProjectReference>
                <Document>
                    public class CSClass
                    {
                        public void Foo()
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

            Test(input, expected)
        End Sub

    End Class
End Namespace
