' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.GenerateVariable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.GenerateVariable
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateVariable
    Partial Public Class GenerateVariableCrossLanguageUserDiagnosticTests
        Inherits AbstractCrossLanguageUserDiagnosticTests

        Public Sub New(outputHelper As ITestOutputHelper)
            MyBase.New(outputHelper)
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As (DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return (Nothing, New CSharpGenerateVariableCodeFixProvider())
            Else
                Return (Nothing, New VisualBasicGenerateVariableCodeFixProvider())
            End If
        End Function

        Protected Overrides Function MassageActions(actions As IList(Of CodeAction)) As IList(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestSimpleInstanceProperty_VisualBasicToCSharp() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                <ProjectReference>CSAssembly1</ProjectReference>
                <Document>
                    public class VBClass
                        public sub Goo()
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
