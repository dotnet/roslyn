' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.CaseCorrection
    <UseExportProvider>
    Public Class VisualBasicCaseCorrectionTestBase

        Protected Shared Async Function AssertCaseCorrectAsync(code As String, expected As String) As Task
            Using workspace = New AdhocWorkspace()
                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(code))

                Dim newNode = Await CaseCorrector.CaseCorrectAsync(document, New TextSpan(0, code.Length))
                Dim actual = Await newNode.GetTextAsync()

                AssertEx.EqualOrDiff(expected, actual.ToString())
            End Using
        End Function

    End Class
End Namespace
