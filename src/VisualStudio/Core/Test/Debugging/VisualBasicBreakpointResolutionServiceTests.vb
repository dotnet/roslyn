' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging
    Public Class VisualBasicBreakpointResolutionServiceTests

        Public Async Function TestSpanWithLengthAsync(markup As XElement, length As Integer) As Task
            Dim position As Integer? = Nothing
            Dim expectedSpan As TextSpan? = Nothing
            Dim source As String = Nothing
            MarkupTestFile.GetPositionAndSpan(markup.NormalizedValue, source, position, expectedSpan)

            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(source)
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

        <WorkItem(876520)>
        <Fact>
        Public Async Function TestBreakpointSpansMultipleMethods() As Task
            ' Normal case: debugger passing BP spans "sub Foo() end sub"
            Await TestSpanWithLengthAsync(<text>
class C
  [|$$sub Foo()|]
  end sub

  sub Bar()
  end sub
end class</text>, 20)

            ' Rare case: debugger passing BP spans "sub Foo() end sub sub Bar() end sub"
            Await TestSpanWithLengthAsync(<text>
class C
  $$sub Foo()
  end sub

  [|sub Bar()|]
  end sub
end class</text>, 35)
        End Function
    End Class
End Namespace
