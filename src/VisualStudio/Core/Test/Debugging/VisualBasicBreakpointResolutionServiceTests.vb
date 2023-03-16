' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging
    <[UseExportProvider]>
    Public Class VisualBasicBreakpointResolutionServiceTests

        Public Async Function TestSpanWithLengthAsync(markup As XElement, length As Integer) As Task
            Dim position As Integer? = Nothing
            Dim expectedSpan As TextSpan? = Nothing
            Dim source As String = Nothing
            MarkupTestFile.GetPositionAndSpan(markup.NormalizedValue, source, position, expectedSpan)

            Using workspace = TestWorkspace.CreateVisualBasic(source)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim result As BreakpointResolutionResult = Await VisualBasicBreakpointResolutionService.GetBreakpointAsync(document, position.Value, length, CancellationToken.None)
                Assert.True(expectedSpan.Value = result.TextSpan,
                                String.Format(vbCrLf & "Expected: {0} ""{1}""" & vbCrLf & "Actual: {2} ""{3}""",
                                                expectedSpan.Value,
                                                source.Substring(expectedSpan.Value.Start, expectedSpan.Value.Length),
                                                result.TextSpan,
                                                source.Substring(result.TextSpan.Start, result.TextSpan.Length)))
            End Using
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876520")>
        Public Async Function TestBreakpointSpansMultipleMethods() As Task
            ' Normal case: debugger passing BP spans "sub Goo() end sub"
            Await TestSpanWithLengthAsync(<text>
class C
  [|$$sub Goo()|]
  end sub

  sub Bar()
  end sub
end class</text>, 20)

            ' Rare case: debugger passing BP spans "sub Goo() end sub sub Bar() end sub"
            Await TestSpanWithLengthAsync(<text>
class C
  $$sub Goo()
  end sub

  [|sub Bar()|]
  end sub
end class</text>, 35)
        End Function
    End Class
End Namespace
