' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class OverallOutliningTests
        Inherits AbstractOutlinerTests

#If False Then
        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Sub DirectivesAtEndOfFile()
            Dim code = "Class C" & vbCrLf &
"End Class" & vbCrLf &
"" & vbCrLf &
"#Region ""Something""" & vbCrLf &
"#End Region"

            VerifyRegions(code,
                          New OutliningSpan(TextSpan.FromBounds(0, 18), "Class C ...", autoCollapse:=False),
                          New OutliningSpan(TextSpan.FromBounds(22, 54), "Something", autoCollapse:=False))
        End Sub
#End If

        Private Async Function VerifyRegionsAsync(code As String, ParamArray expectedRegions As OutliningSpan()) As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync(code)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Dim outliningService = document.Project.LanguageServices.GetService(Of IOutliningService)()
                Dim actualRegions = outliningService.GetOutliningSpansAsync(document, CancellationToken.None).WaitAndGetResult(CancellationToken.None).ToList()

                Assert.Equal(expectedRegions.Length, actualRegions.Count)

                For i = 0 To expectedRegions.Length - 1
                    AssertRegion(expectedRegions(i), actualRegions(i))
                Next
            End Using
        End Function
    End Class
End Namespace
