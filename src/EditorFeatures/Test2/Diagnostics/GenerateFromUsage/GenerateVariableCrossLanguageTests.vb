' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateVariable
    Partial Public Class GenerateVariableCrossLanguageTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateVariable.GenerateVariableCodeFixProvider())
            Else
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateVariable.GenerateVariableCodeFixProvider())
            End If
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestSimpleInstanceProperty_VisualBasicToCSharp() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
                    public class VBClass
                        public sub Foo()
                            Dim v as CSClass
                            Dim x As String = v.$$Bar
                        end sub
                    end class
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                <Document FilePath=<%= DestinationDocument %>>
                    public class CSClass
                    {
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                    public class CSClass
                    {
                        public string Bar { get; set; }
                    }
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function
    End Class
End Namespace
